using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Utils
{
    /// <summary>
    /// Small utility for reading JSON files and extracting values by JSONPath (SelectToken).
    /// </summary>
    /// <remarks>
    /// - Targets .NET Framework 4.8 / C# 7.3.
    /// - Depends on <c>Newtonsoft.Json</c> (Json.NET). Ensure the consumer project references the package.
    /// - Provide <c>ReadJsonFile</c> when callers require strict parsing and exceptions on failure.
    /// - Use <c>TryGetValue</c>/<c>GetValue</c> for tolerant, non-throwing lookups.
    /// </remarks>
    public static class JsonFileReader
    {
        /// <summary>
        /// Reads and parses a JSON file into a <see cref="JToken"/>.
        /// </summary>
        /// <param name="filePath">Path to the JSON file. Must be a non-empty path.</param>
        /// <returns>The parsed <see cref="JToken"/> root of the JSON document.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="JsonException">Thrown when the file content cannot be parsed as valid JSON.</exception>
        /// <remarks>
        /// This method performs a strict read & parse: it will throw on missing or invalid files.
        /// Use this when callers need to observe and handle parse errors directly.
        /// </remarks>
        /// <example>
        /// Example:
        /// <code>
        /// var root = JsonFileReader.ReadJsonFile("config.json");
        /// var id = root.SelectToken("meta.id")?.ToString() ?? string.Empty;
        /// </code>
        /// </example>
        public static JToken ReadJsonFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("JSON file not found", filePath);

            var txt = File.ReadAllText(filePath);
            return JToken.Parse(txt);
        }

        /// <summary>
        /// Attempts to get a flattened string value from a JSON file for the provided JSON path.
        /// </summary>
        /// <param name="filePath">Path to the JSON file to read. If invalid/missing the method returns <c>false</c>.</param>
        /// <param name="jsonPath">
        /// JSON path or property name to select. This uses <see cref="JToken.SelectToken(string,bool)"/>.
        /// If <c>null</c> or whitespace, the entire document root is used and flattened.
        /// Examples: <c>"property"</c>, <c>"profile.name"</c>, <c>"$.items[0].id"</c>.
        /// </param>
        /// <param name="value">
        /// Output parameter that receives the flattened string representation of the selected token.
        /// On failure the parameter will equal <paramref name="defaultValue"/>.
        /// </param>
        /// <param name="defaultValue">Value to return in <paramref name="value"/> when the token is missing or an error occurs. Defaults to empty string.</param>
        /// <returns>
        /// <c>true</c> if a token was found and flattened successfully (even if the flattened value is an empty string); otherwise <c>false</c>.
        /// Returns <c>false</c> on missing token, file problems, or parsing errors.
        /// </returns>
        /// <remarks>
        /// - This method swallows exceptions (I/O and parse errors) and returns <c>false</c> so callers can perform tolerant lookups.
        /// - If you need to observe exceptions for missing/invalid files, call <see cref="ReadJsonFile"/> directly.
        /// - Flattening rules:
        ///   - Arrays -> elements flattened recursively and joined with ';'
        ///   - Objects -> compact JSON string (no pretty formatting)
        ///   - Primitives -> token's string value
        ///   - null -> empty string
        /// </remarks>
        /// <example>
        /// Example:
        /// <code>
        /// if (JsonFileReader.TryGetValue("config.json", "roles", out string roles, "none"))
        /// {
        ///     // roles might be "admin;user" if JSON contains an array
        /// }
        /// else
        /// {
        ///     // file missing/invalid or token not found
        /// }
        /// </code>
        /// </example>
        public static bool TryGetValue(string filePath, string jsonPath, out string value, string defaultValue = "")
        {
            value = defaultValue;
            try
            {
                var root = ReadJsonFile(filePath);
                JToken token = string.IsNullOrWhiteSpace(jsonPath)
                    ? root
                    : root.SelectToken(jsonPath, errorWhenNoMatch: false);

                if (token == null) return false;

                value = FlattenJToken(token);
                return true;
            }
            catch
            {
                // swallow exceptions and return false so callers can handle missing/invalid files gracefully
                value = defaultValue;
                return false;
            }
        }

        /// <summary>
        /// Convenience wrapper: returns the flattened token value or <paramref name="defaultValue"/> when not found or on error.
        /// </summary>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <param name="jsonPath">JSON path or property name to select (see <see cref="TryGetValue"/> for semantics).</param>
        /// <param name="defaultValue">Default to return when not found or on error.</param>
        /// <returns>Flattened string value or <paramref name="defaultValue"/> when token is missing or an error occurs.</returns>
        /// <example>
        /// <code>
        /// string name = JsonFileReader.GetValue("config.json", "profile.name", "unknown");
        /// </code>
        /// </example>
        public static string GetValue(string filePath, string jsonPath, string defaultValue = "")
        {
            string v;
            return TryGetValue(filePath, jsonPath, out v, defaultValue) ? v : defaultValue;
        }

        /// <summary>
        /// Flattens a <see cref="JToken"/> into a single string according to the module's flattening rules:
        /// - Primitive tokens -> their string representation.
        /// - Arrays -> elements flattened recursively and joined with ';'.
        /// - Objects -> returned as compact JSON (no formatting).
        /// - Null tokens -> empty string.
        /// </summary>
        /// <param name="token">Token to flatten.</param>
        /// <returns>Flattened string representation (never <c>null</c>; empty string for null tokens).</returns>
        /// <remarks>
        /// This helper centralizes how token values are converted to strings for the public APIs.
        /// </remarks>
        private static string FlattenJToken(JToken token)
        {
            if (token == null) return string.Empty;

            switch (token.Type)
            {
                case JTokenType.Array:
                    var arr = (JArray)token;
                    return string.Join(";", arr.Select(FlattenJToken));
                case JTokenType.Object:
                    // return compact JSON for objects
                    return token.ToString(Formatting.None);
                case JTokenType.Null:
                    return string.Empty;
                default:
                    // For primitives use the value as string using invariant culture where applicable
                    var val = token.ToString(Formatting.None);
                    return val ?? string.Empty;
            }
        }
    }
}