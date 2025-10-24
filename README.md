# Common.Utils

Small utilities for normalizing filesystem paths and preparing file locations used across the project.

## Overview
`Common.Utils.File_Utils` provides two static helpers:

- `File_Utils.NormalizePath(string path)` — resilient path normalization (expands environment variables, trims quotes/whitespace, converts separators, tries to resolve full path, removes trailing separators except root). Returns `string.Empty` for null/whitespace input.
- `File_Utils.SetFile(string value, string ConfiguredFolder)` — builds a target file path optionally rooted at `ConfiguredFolder`, creates the folder(s) if needed, ensures the file's directory exists, and stores the resulting path in the static `File_Utils.FilePath` property.

Notes:
- The class performs path normalization and directory creation only; it does not read or write file contents.
- `FilePath` is a last-set global static and is not synchronized — if your code uses multiple threads, consider synchronizing access or avoiding reliance on this global.

## Requirements
- .NET Framework 4.8
- C# 7.3 compatible compiler (project is configured for C# 7.3)

## Quick usage

## Build / Documentation Tips
- To produce XML documentation for consumers, open __Project Properties__ → __Build__ and check __XML documentation file__.
- For browsable API docs consider DocFX or Sandcastle; enabling XML docs will allow those tools to generate richer output.

## Contributing
- Follow the existing code style (snake-style class name `File_Utils` is intentional for consistency).
- Keep helpers small and focused; avoid introducing I/O beyond directory creation in this utility.

## License
This project is licensed under the McSpidy License — see the accompanying `LICENSE` file for full terms.
