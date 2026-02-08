using CodeLogic.Logging;
using CL.Mail.Models;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CL.Mail.Services;

/// <summary>
/// Template engine with variable substitution, conditionals, loops, and layout support.
///
/// Supported syntax:
///   {{variable}}              - Variable substitution
///   ${variable}               - Variable substitution (alt syntax)
///   {variable}                - Variable substitution (legacy)
///   {{#if var}}...{{/if}}     - Conditional block
///   {{#if var}}...{{#else}}...{{/if}} - Conditional with else
///   {{#each items}}...{{/each}}      - Loop over collection
///   {{#section name}}...{{/section}} - Named section (for layouts)
/// </summary>
public class SimpleTemplateEngine : IMailTemplateEngine
{
    private readonly ILogger _logger;
    private readonly IMailTemplateProvider? _templateProvider;

    /// <summary>
    /// Gets the name of this template engine
    /// </summary>
    public string Name => "Simple";

    /// <summary>
    /// Initializes a new instance of the SimpleTemplateEngine
    /// </summary>
    public SimpleTemplateEngine(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance with layout support via a template provider
    /// </summary>
    public SimpleTemplateEngine(ILogger logger, IMailTemplateProvider templateProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
    }

    /// <summary>
    /// Renders a mail template by processing layouts, conditionals, loops, and variables
    /// </summary>
    public async Task<MailResult<RenderedTemplate>> RenderAsync(
        MailTemplate template,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subject = RenderText(template.Subject, variables);
            var textBody = template.TextBody != null ? RenderText(template.TextBody, variables) : null;
            var htmlBody = template.HtmlBody != null ? RenderText(template.HtmlBody, variables) : null;

            // Apply layout if specified
            if (!string.IsNullOrWhiteSpace(template.Layout) && _templateProvider != null)
            {
                var layoutResult = await _templateProvider.LoadTemplateAsync(template.Layout, cancellationToken).ConfigureAwait(false);
                if (layoutResult.IsSuccess && layoutResult.Value != null)
                {
                    var layout = layoutResult.Value;

                    // Extract sections from the rendered child content
                    var textSections = textBody != null ? ExtractSections(textBody) : new Dictionary<string, string>();
                    var htmlSections = htmlBody != null ? ExtractSections(htmlBody) : new Dictionary<string, string>();

                    // Add rendered child body as "body" section
                    textSections.TryAdd("body", StripSections(textBody ?? string.Empty));
                    htmlSections.TryAdd("body", StripSections(htmlBody ?? string.Empty));

                    // Merge sections into variables for the layout
                    var layoutVars = new Dictionary<string, object?>(variables);
                    foreach (var section in htmlSections)
                        layoutVars.TryAdd(section.Key, section.Value);

                    // Render the layout templates
                    if (layout.TextBody != null)
                        textBody = RenderText(layout.TextBody, layoutVars);
                    if (layout.HtmlBody != null)
                        htmlBody = RenderText(layout.HtmlBody, layoutVars);
                }
                else
                {
                    _logger.Warning($"Layout template '{template.Layout}' not found, rendering without layout");
                }
            }

            var rendered = new RenderedTemplate
            {
                Subject = subject,
                TextBody = textBody,
                HtmlBody = htmlBody
            };

            return MailResult<RenderedTemplate>.Success(rendered);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Template rendering was cancelled");
            return MailResult<RenderedTemplate>.Failure(MailError.TemplateRenderingFailed, "Rendering cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error rendering template '{template.Id}'", ex);
            return MailResult<RenderedTemplate>.Failure(
                MailError.TemplateRenderingFailed,
                ex.Message);
        }
    }

    /// <summary>
    /// Multi-pass rendering pipeline: conditionals, loops, then variable substitution
    /// </summary>
    private static string RenderText(string text, Dictionary<string, object?> variables)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text;

        // Pass 1: Process conditionals (innermost first, iterative)
        result = ProcessConditionals(result, variables);

        // Pass 2: Process loops
        result = ProcessLoops(result, variables);

        // Pass 3: Variable substitution
        result = SubstituteVariables(result, variables);

        return result;
    }

    /// <summary>
    /// Processes {{#if var}}...{{/if}} and {{#if var}}...{{#else}}...{{/if}} blocks.
    /// Handles nesting by processing innermost blocks first.
    /// </summary>
    private static string ProcessConditionals(string text, Dictionary<string, object?> variables)
    {
        // Iteratively process innermost {{#if}} blocks until none remain
        var maxIterations = 100;
        var iteration = 0;

        while (iteration++ < maxIterations)
        {
            // Match innermost {{#if var}}...{{/if}} (no nested #if inside)
            var match = Regex.Match(text,
                @"\{\{#if\s+(\w+)\}\}((?:(?!\{\{#if\b).)*?)\{\{/if\}\}",
                RegexOptions.Singleline);

            if (!match.Success)
                break;

            var varName = match.Groups[1].Value;
            var innerContent = match.Groups[2].Value;

            var isTruthy = IsTruthy(variables, varName);

            // Check for {{#else}} inside this block
            var elseIndex = innerContent.IndexOf("{{#else}}", StringComparison.Ordinal);
            string replacement;

            if (elseIndex >= 0)
            {
                var trueBlock = innerContent[..elseIndex];
                var falseBlock = innerContent[(elseIndex + "{{#else}}".Length)..];
                replacement = isTruthy ? trueBlock : falseBlock;
            }
            else
            {
                replacement = isTruthy ? innerContent : string.Empty;
            }

            text = text[..match.Index] + replacement + text[(match.Index + match.Length)..];
        }

        return text;
    }

    /// <summary>
    /// Processes {{#each items}}...{{/each}} blocks.
    /// Collection items can be Dictionary&lt;string, object?&gt; or objects (reflection).
    /// </summary>
    private static string ProcessLoops(string text, Dictionary<string, object?> variables)
    {
        var maxIterations = 100;
        var iteration = 0;

        while (iteration++ < maxIterations)
        {
            var match = Regex.Match(text,
                @"\{\{#each\s+(\w+)\}\}(.*?)\{\{/each\}\}",
                RegexOptions.Singleline);

            if (!match.Success)
                break;

            var collectionName = match.Groups[1].Value;
            var itemTemplate = match.Groups[2].Value;

            var sb = new StringBuilder();

            if (TryGetVariable(variables, collectionName, out var collectionObj) && collectionObj is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    // Build per-iteration variables by merging item properties into parent scope
                    var iterVars = new Dictionary<string, object?>(variables, StringComparer.OrdinalIgnoreCase);

                    if (item is Dictionary<string, object?> dict)
                    {
                        foreach (var kvp in dict)
                            iterVars[kvp.Key] = kvp.Value;
                    }
                    else if (item is IDictionary<string, object> dictObj)
                    {
                        foreach (var kvp in dictObj)
                            iterVars[kvp.Key] = kvp.Value;
                    }
                    else if (item != null)
                    {
                        // Use reflection to get public properties
                        foreach (var prop in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            try { iterVars[prop.Name] = prop.GetValue(item); }
                            catch { /* skip inaccessible properties */ }
                        }
                    }

                    // Render this iteration (recursively process conditionals, loops, and vars)
                    var rendered = ProcessConditionals(itemTemplate, iterVars);
                    rendered = ProcessLoops(rendered, iterVars);
                    rendered = SubstituteVariables(rendered, iterVars);
                    sb.Append(rendered);
                }
            }

            text = text[..match.Index] + sb.ToString() + text[(match.Index + match.Length)..];
        }

        return text;
    }

    /// <summary>
    /// Replaces variables using {{var}}, ${var}, and {var} syntax
    /// </summary>
    private static string SubstituteVariables(string text, Dictionary<string, object?> variables)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text;

        // Replace {{variable}} format
        result = Regex.Replace(result, @"\{\{(\w+)\}\}", match =>
        {
            var key = match.Groups[1].Value;
            return TryGetVariable(variables, key, out var value) ? (value?.ToString() ?? string.Empty) : match.Value;
        });

        // Replace ${variable} format
        result = Regex.Replace(result, @"\$\{(\w+)\}", match =>
        {
            var key = match.Groups[1].Value;
            return TryGetVariable(variables, key, out var value) ? (value?.ToString() ?? string.Empty) : match.Value;
        });

        // Replace {variable} format (legacy) — avoid matching {{var}} double-braces
        result = Regex.Replace(result, @"(?<!\{)\{(\w+)\}(?!\})", match =>
        {
            var key = match.Groups[1].Value;
            return TryGetVariable(variables, key, out var value) ? (value?.ToString() ?? string.Empty) : match.Value;
        });

        return result;
    }

    /// <summary>
    /// Determines if a variable is truthy.
    /// Falsy: null, empty string, "false", "0", false, 0
    /// </summary>
    private static bool IsTruthy(Dictionary<string, object?> variables, string varName)
    {
        if (!TryGetVariable(variables, varName, out var value))
            return false;

        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s)
                && !s.Equals("false", StringComparison.OrdinalIgnoreCase)
                && s != "0",
            _ => true
        };
    }

    /// <summary>
    /// Case-insensitive variable lookup
    /// </summary>
    private static bool TryGetVariable(Dictionary<string, object?> variables, string key, out object? value)
    {
        // Direct lookup
        if (variables.TryGetValue(key, out value))
            return true;

        // Case-insensitive fallback
        foreach (var kvp in variables)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kvp.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Extracts named sections from content: {{#section name}}content{{/section}}
    /// </summary>
    private static Dictionary<string, string> ExtractSections(string text)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var matches = Regex.Matches(text, @"\{\{#section\s+(\w+)\}\}(.*?)\{\{/section\}\}", RegexOptions.Singleline);
        foreach (Match match in matches)
        {
            sections[match.Groups[1].Value] = match.Groups[2].Value.Trim();
        }

        return sections;
    }

    /// <summary>
    /// Removes section blocks from content, leaving only non-section content
    /// </summary>
    private static string StripSections(string text)
    {
        return Regex.Replace(text, @"\{\{#section\s+\w+\}\}.*?\{\{/section\}\}", string.Empty, RegexOptions.Singleline).Trim();
    }
}
