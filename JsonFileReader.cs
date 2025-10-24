using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Utils
{
    /// <summary>
    /// Small utility for reading JSON files and extracting values by JSONPath (SelectToken).
    /// Targets .NET Framework 4.8 / C# 7.3 and depends on Newtonsoft.Json (Json.NET).
    /// </summary>
    public static class JsonFileReader
    {
        /// <summary>
        /// Reads and parses a JSON file into a <see cref="JToken"/>.
        /// </summary>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <returns>Parsed <see cref="JToken"/> root.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
        /// <exception cref="JsonException">If the file cannot be parsed as JSON.</exception>
        public static JToken ReadJsonFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("JSON file not found", filePath);

            var txt = File.ReadAllText(filePath);
            return JToken.Parse(txt);
        }

        /// <summary>
        /// Attempts to get a flattened string value from a JSON file for the provided JSONPath.
        /// JSONPath is the token path used by <see cref="JToken.SelectToken(string,bool)"/>.
        /// Example paths: "property", "profile.name", "$.items[0].id"
        /// </summary>
        /// <param name="filePath">Path to JSON file.</param>
        /// <param name="jsonPath">JSONPath or property name to select. If null/empty the entire document is returned (flattened).</param>
        /// <param name="value">Output flattened string value (or defaultValue on failure).</param>
        /// <param name="defaultValue">Value returned when token is missing or conversion fails.</param>
        /// <returns>True if a token was found (even if it's an empty string); false on error or missing token.</returns>
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
        /// Convenience: get value or default (no exceptions thrown).
        /// </summary>
        /// <param name="filePath">Path to JSON file.</param>
        /// <param name="jsonPath">JSONPath or property name to select.</param>
        /// <param name="defaultValue">Default to return when not found or on error.</param>
        /// <returns>Flattened string value or <paramref name="defaultValue"/>.</returns>
        public static string GetValue(string filePath, string jsonPath, string defaultValue = "")
        {
            string v;
            return TryGetValue(filePath, jsonPath, out v, defaultValue) ? v : defaultValue;
        }

        /// <summary>
        /// Flattens a JToken into a single string:
        /// - Primitive tokens -> their string value.
        /// - Arrays -> elements joined by ';' (elements flattened recursively).
        /// - Objects -> compact JSON string (no formatting).
        /// </summary>
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