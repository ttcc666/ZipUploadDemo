# ZipUploadDemo AGENTS.md

## Build Commands

```bash
# Build project
dotnet build

# Build specific configuration
dotnet build --configuration Release

# Run the API (from ZipUploadDemo.Api directory)
dotnet run

# Restore dependencies
dotnet restore
```

## Code Style Guidelines

### File Structure & Organization
- Use **file-scoped namespaces** (C# 10+ feature): `namespace ZipUploadDemo.Api.Controllers;`
- Place using statements at top, organized: System → Third-party → Project namespaces
- One public class per file, filename matches class name

### Naming Conventions
- **Classes/Interfaces**: PascalCase (`UploadController`, `ISqlSugarClient`)
- **Methods/Properties**: PascalCase (`ProcessAsync`, `BatchId`)
- **Private fields**: `_camelCase` prefix (`_processing`, `_db`)
- **Parameters**: camelCase (`cancellationToken`, `safeFileName`)
- **Async methods**: suffix with `Async` (`ProcessAsync`)
- **Constants**: PascalCase (`MaxRequestBodySize`)

### Types & Nullable
- Enable nullable reference types (`<Nullable>enable</Nullable>` in csproj)
- Use `string?` for potentially null strings
- Use `List<T>` or `IReadOnlyList<T>` for collections
- Use `IEnumerable<T>` for read-only collection parameters

### Error Handling
- Use try-catch blocks with `ILogger.LogError` for exceptions
- Return appropriate HTTP status codes (400 for client errors, 500 for server errors)
- Include descriptive error messages
- Always log exceptions with context

### Code Patterns
- Constructor injection for dependencies (`ILogger<T>`, `ISqlSugarClient`, etc.)
- XML documentation for public APIs (`/// <summary>`)
- Use `async`/`await` with `CancellationToken` for cancelable operations
- Use `DateTime.UtcNow` for timestamps
- Use `string.IsNullOrWhiteSpace()` for null/empty checks
- Use `Path.Combine()` for file paths
- Log with structured logging: `LogInformation("Message: {Param}", value)`

### Imports Order (example)
```csharp
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
using SqlSugar;
using ZipUploadDemo.Api.Dtos;
using ZipUploadDemo.Api.Models;
using ZipUploadDemo.Api.Options;
using ZipUploadDemo.Api.Services;
```

### Configuration
- Use `IOptions<T>` for configuration binding
- Use `appsettings*.json` for environment-specific settings
- Use `builder.Configuration.GetSection("Key")` for configuration access

### Database (SqlSugar)
- Use `ISqlSugarClient` for DB operations
- Use `CodeFirst.InitTables<T>()` for schema initialization
- Use `BulkCopyAsync` for bulk inserts
- Wrap transactions with `BeginTran()`/`CommitTran()`/`RollbackTran()`
