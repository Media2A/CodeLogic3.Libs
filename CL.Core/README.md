# CL.Core - Core Utilities Library

**Version:** 3.0.0
**CodeLogic 3.0 Compatible** ✅

CL.Core is a comprehensive utility collection for CodeLogic 3.0, providing essential helpers for image manipulation, web operations, security, networking, and more.

## 📦 Features

### 🖼️ **Image Utilities** (`Utilities/Imaging/`)
- `Image.cs` - Image manipulation (resize, crop, rotate, watermark)
- `Convert.cs` - Format conversion (PNG, JPEG, GIF, BMP, WebP)

### 🌐 **Web Utilities** (`Utilities/Web/`)
- `Api.cs` - RESTful API client helpers
- `Client.cs` - HTTP client wrapper
- `WebRequest.cs` - Advanced web request builder
- `Request.cs` / `Response.cs` - HTTP request/response helpers
- `Url.cs` - URL parsing and manipulation
- `Cookies.cs` - Cookie management
- `Session.cs` - Session handling
- `Context.cs` - HTTP context utilities
- `HttpHeaders.cs` - Header manipulation
- `HtmlTrimmer.cs` - HTML content trimming
- `SanitizeHtml.cs` - HTML sanitization and XSS prevention

### 🔐 **Security Utilities** (`Utilities/Security/`)
- `Encryption.cs` - AES, RSA encryption/decryption
- `Hashing.cs` - MD5, SHA-256, SHA-512, bcrypt hashing
- `PasswordGenerator.cs` - Secure password generation

### 🗜️ **Compression** (`Utilities/Compression/`)
- `CompressionHelper.cs` - GZip, LZ4 compression/decompression

### 📝 **String & Validation** (`Utilities/StringNumeric/`)
- `StringHelper.cs` - String manipulation utilities
- `StringValidator.cs` - Email, phone, URL validation

### 📂 **File Handling** (`Utilities/FileHandling/`)
- `FileSystem.cs` - File I/O operations, directory management

### 📅 **Date & Time** (`Utilities/TimeDate/`)
- `DateTimeHelper.cs` - Date formatting, parsing, timezone conversion

### 🔧 **Type Conversion** (`Utilities/Conversion/`)
- `TypeConverter.cs` - Safe type conversions (string → int, etc.)

### 📊 **Data Utilities** (`Utilities/Data/`)
- `JsonHelper.cs` - JSON serialization/deserialization

### 🎲 **Generators** (`Utilities/Generators/`)
- `IdGenerator.cs` - GUID, unique ID generation
- `PasswordGenerator.cs` - Strong password generation

### 🌐 **Networking** (`Utilities/Networking/`)
- `Ping.cs` - Network ping utility
- `NSLookup.cs` - DNS lookup
- `TraceRoute.cs` - Network route tracing
- `SubnetCalculator.cs` - IP subnet calculations

### ⏰ **Parsing** (`Utilities/Parser/`)
- `Cron.cs` - Cron expression parsing

### 🔍 **Assembly & Reflection** (`Utilities/Assemblies/`)
- `AssemblyHelper.cs` - Assembly loading and inspection
- `AssemblyInfo.cs` - Assembly metadata
- `AssemblyLoader.cs` - Dynamic assembly loading
- `ReflectionHelper.cs` - Reflection utilities
- `Classes.cs` - Class discovery and instantiation
- `Instances.cs` - Object instantiation helpers
- `Invoke.cs` - Method invocation
- `ObjectViewer.cs` - Object inspection
- `Resources.cs` - Embedded resource access
- `NetFramework.cs` - .NET framework utilities

## 🚀 Usage

### Auto-Discovery (Recommended)
CL.Core is automatically discovered and loaded by CodeLogic 3.0:

```csharp
// Initialize framework
await CodeLogic.CodeLogic.InitializeAsync();
await CodeLogic.CodeLogic.ConfigureAsync();  // Auto-discovers CL.Core
await CodeLogic.CodeLogic.StartAsync();

// Get CL.Core library
var core = Libs.Get<CoreLibrary>();
```

### Accessing Utilities
All utilities are static classes under the `CL.Core.Utilities` namespace:

```csharp
using CL.Core.Utilities.Imaging;
using CL.Core.Utilities.Web;
using CL.Core.Utilities.Security;

// Image manipulation
CLU_Image.Resize("input.jpg", "output.jpg", 800, 600);

// Web request
var response = await CLU_Web.ClientRequestAsync("https://api.example.com/data");

// Hashing
var hash = CLU_Hashing.SHA256("password123");

// Password generation
var password = CLU_PasswordGenerator.Generate(16, includeSpecial: true);
```

## 📁 Folder Structure

```
Demo.App/bin/Debug/net10.0/
├── CL.Core.dll                    # Library DLL in root
├── CodeLogic.dll
│
└── CodeLogic/
    └── CL.core/                   # Isolated library directory
        ├── config.json            # Library configuration
        ├── data/                  # Data files
        ├── logs/                  # Library-specific logs
        └── localization/          # Translations
```

## 📝 Dependencies

- **CodeLogic 3.0** - Core framework
- **Magick.NET** - Image manipulation (ImageMagick)
- **Newtonsoft.Json** - JSON serialization
- **K4os.Compression.LZ4** - LZ4 compression
- **StackExchange.Redis** - Redis integration (optional)
- **ASP.NET Core** - Web utilities

## ✅ CodeLogic 3.0 Integration

- ✅ **Auto-Discovery** - Automatically detected by framework
- ✅ **Logging** - Integrated with CodeLogic 3.0 logging system
- ✅ **Configuration** - Config files in `CodeLogic/CL.core/config.json`
- ✅ **Localization** - Multi-language support ready
- ✅ **Lifecycle** - Full lifecycle support (Configure → Initialize → Start → Stop)
- ✅ **Health Check** - Built-in health monitoring

## 🔄 Lifecycle

```
Configure → Initialize → Start → [Running] → Stop
```

CL.Core follows the standard CodeLogic 3.0 library lifecycle with proper logging at each phase.

## 📊 Example Output

```
→ Discovering libraries...
  ✓ Discovered: Core Utilities Library v3.0.0
  ✓ Discovered: MySQL2 Database Library v2.0.0

→ Configuring 2 libraries...
[CORE] 2025-11-22 21:43:04.325 [INFO] Configuring Core Utilities Library v3.0.0
  ✓ Configured: Core Utilities Library

→ Initializing libraries...
[CORE] 2025-11-22 21:43:04.491 [INFO] Initializing Core Utilities Library
[CORE] 2025-11-22 21:43:04.536 [INFO] Core Utilities Library initialized successfully
  ✓ Initialized: Core Utilities Library

→ Starting libraries...
[CORE] 2025-11-22 21:43:04.697 [INFO] Starting Core Utilities Library
[CORE] 2025-11-22 21:43:04.736 [INFO] Core Utilities Library started and ready
  ✓ Started: Core Utilities Library
```

## 🎯 Next Steps

All existing utilities from CodeLogic 2.0 have been migrated to CodeLogic 3.0. The utilities are ready to use with the new logging, configuration, and localization systems.

To add logging to individual utility methods, access the library context:
```csharp
var core = Libs.Get<CoreLibrary>();
var context = core.GetContext();
context.Logger.Info("Operation completed");
```
