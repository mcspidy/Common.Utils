# Common.Utils

Small utilities for normalizing filesystem paths and reading simple values from JSON files. Intended for small helper scenarios used across the repository.

## Modules

This repository contains two primary modules; each is documented below with API, behavior, examples and important notes.

---

### Module: `File_Utils` (implemented in `File_Utils.cs`)

Purpose: small, focused helpers for path normalization and preparing a file path (ensuring the directory exists).

Public surface:
- `public static string FilePath { get; }`
  - The last file path produced by `SetFile`. A last-set global; not thread-safe.
- `public static string SetFile(string value, string ConfiguredFolder)`
  - Builds a target file path and ensures the target directory exists.
  - Behavior:
    - If `ConfiguredFolder` is non-whitespace:
      - `ConfiguredFolder` is normalized (`NormalizePath`), created via `Directory.CreateDirectory`, and combined with `value`.
    - Otherwise, `value` is normalized directly.
    - The final path is assigned to `FilePath`, returned, and the file's parent directory is created (calls `Directory.CreateDirectory`).
    - Directory-creation exceptions (e.g., permission issues) will bubble up.
- `public static string NormalizePath(string path)`
  - Resilient normalization:
    - Returns `string.Empty` for null/whitespace input.
    - Expands environment variables.
    - Trims surrounding quotes and whitespace.
    - Converts alternate separators to the platform default.
    - Attempts to resolve to a full path (wrapped in `try/catch`).
    - Removes trailing separators except for drive/root paths.
    - Tries to return a best-effort string rather than throwing for common path issues.

Examples:

```csharp
// example: typical usage scenario
using Common.Utils;

// raw normalization (expands env vars, resolves "..", etc.):
var normal = File_Utils.NormalizePath(@"%TEMP%\..\MyFolder\..\MyFile.json");

// explicit file targeting (ensures folder exists):
File_Utils.SetFile(@"MyFile.json", @"%TEMP%\MyFolder");


// results in (on disk):
//   C:\Users\You\AppData\Local\Temp\MyFolder\MyFile.json

// and the static FilePath property is set:
var path = File_Utils.FilePath;
```

Notes / caveats:
- `File_Utils` will create directories on disk. It does not read or write file contents.
- `FilePath` is a global last-set property and is not synchronized; synchronize externally if used across threads.
- The class targets .NET Framework 4.8 and C# 7.3.

---

### Module: `JsonFileReader` (implemented in `JsonFileReader.cs`)

Purpose: read JSON files and extract values with JSON path-like selection using `Newtonsoft.Json` (`JToken.SelectToken`).

Public surface:
- `public static JToken ReadJsonFile(string filePath)`
  - Reads and parses `filePath` into a `JToken`.
  - Throws:
    - `ArgumentNullException` if `filePath` is null/empty.
    - `FileNotFoundException` if the file does not exist.
    - `JsonException` (or `JsonReaderException`) if the file is not valid JSON.
  - Use when callers require strict parsing and exceptions on failures.
- `public static bool TryGetValue(string filePath, string jsonPath, out string value, string defaultValue = "")`
  - Safe lookup that swallows errors and returns `false` on failure.
  - Behavior:
    - Reads and parses the file, then selects the token with `JToken.SelectToken(jsonPath, errorWhenNoMatch: false)`.
    - If `jsonPath` is null/empty, the entire document (`root`) is used.
    - If token found, returns `true` and `value` receives the flattened string.
    - On error or missing token, returns `false` and `value` is `defaultValue`.
  - Does not throw for missing/invalid files — useful for tolerant lookups.
- `public static string GetValue(string filePath, string jsonPath, string defaultValue = "")`
  - Convenience wrapper returning the found flattened value or `defaultValue` (no exceptions).

Flattening rules (what `GetValue`/`TryGetValue` return):
- Arrays -> elements flattened recursively and joined with `;` (e.g., `[1,2]` -> `"1;2"`).
- Objects -> compact JSON string (no formatting), e.g., `{"a":1}` -> `{"a":1}`.
- Primitives -> the token's string value.
- `null` -> empty string.

Examples:

```csharp
// example: reading a JSON file and extracting a value
using Common.Utils;

// assuming a file "config.json" with content: { "Key": "Value" }
JsonFileReader.ReadJsonFile("config.json")
    .TryGetValue("Key", out string value, "default");

// result: value == "Value"

// safe lookup returns 'true' even for missing/empty values:
JsonFileReader.TryGetValue("config.json", "MissingKey", out string emptyValue, "default");

// result: emptyValue == "default"
```

Notes:
- `JsonFileReader.ReadJsonFile` intentionally throws for missing/invalid files so callers that require strict parsing can receive exceptions; `TryGetValue`/`GetValue` provide non-throwing alternatives.

---

## Requirements

- .NET Framework 4.8
- C# 7.3 compatible compiler (project is configured for C# 7.3)
- `Newtonsoft.Json` (Json.NET) for `JsonFileReader`

## Build / Documentation Tips

- To produce XML documentation for consumers, open __Project Properties__ → __Build__ and check __XML documentation file__.
- For browsable API docs consider DocFX or Sandcastle; enabling XML docs will allow those tools to generate richer output.

## Contributing

- Follow the existing code style (the `File_Utils` snake-style class name is intentional for consistency).
- Keep helpers small and focused; avoid introducing I/O beyond directory creation in `File_Utils`.

## License

This project is licensed under the McSpidy License — see the accompanying `LICENSE` file for full terms.
