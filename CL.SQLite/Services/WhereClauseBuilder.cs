using System.Text;
using CL.SQLite.Models;

namespace CL.SQLite.Services;

internal static class WhereClauseBuilder
{
    public static (string Clause, Dictionary<string, object?> Parameters) Build(IReadOnlyList<WhereCondition> conditions)
    {
        var sb = new StringBuilder();
        var parameters = new Dictionary<string, object?>();

        for (int i = 0; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            if (i > 0)
            {
                sb.Append($" {condition.LogicalOperator} ");
            }

            if (condition.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase) &&
                condition.Value is Array arr)
            {
                var paramNames = new List<string>();
                for (int j = 0; j < arr.Length; j++)
                {
                    var name = $"@p{i}_{j}";
                    paramNames.Add(name);
                    parameters[name] = arr.GetValue(j) ?? DBNull.Value;
                }

                sb.Append($"{condition.Column} IN ({string.Join(", ", paramNames)})");
            }
            else
            {
                var name = $"@p{i}";
                sb.Append($"{condition.Column} {condition.Operator} {name}");
                parameters[name] = condition.Value ?? DBNull.Value;
            }
        }

        return (sb.ToString(), parameters);
    }
}
