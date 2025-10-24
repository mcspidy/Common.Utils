using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Utils
{
    /// <summary>
    /// Collection of simple file-path helper utilities used across the project.
    /// </summary>
    /// <remarks>
    /// - All members are static and operate on strings; no instance is required.
    /// - The <see cref="FilePath"/> property holds the last value returned by <see cref="SetFile(string,string)"/>.
    /// - Methods attempt to avoid throwing for common path issues and return sensible defaults (for example, empty string for null/whitespace input).
    /// - This class is not concerned with file contents; it only normalizes and prepares filesystem paths and directories.
    /// - Targets: .NET Framework 4.8 / C# 7.3.
    /// </remarks>
    public class File_Utils
    {
        /// <summary>
        /// The last file path produced by <see cref="SetFile(string,string)"/>.
        /// </summary>
        /// <value>Normalized file path or empty string if none has been set.</value>
        /// <remarks>
        /// This is a last-set global and is not synchronized. If multiple threads use <see cref="SetFile(string,string)"/>,
        /// callers should synchronize access to avoid race conditions.
        /// </remarks>
        public static string FilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Produces a target file path using an optional configured folder and ensures its directory exists.
        /// </summary>
        /// <param name="value">Filename or path to set. Can be relative or absolute. May include environment variables.</param>
        /// <param name="ConfiguredFolder">
        /// Optional folder to place the file into. If provided and not whitespace:
        /// - it will be normalized,
        /// - the folder will be created (<see cref="Directory.CreateDirectory(string)"/>),
        /// - the returned path will be the combination of that folder and <paramref name="value"/>.
        /// If not provided, <paramref name="value"/> is normalized and returned as-is.
        /// </param>
        /// <returns>
        /// The normalized full path for the target file. Also assigned to <see cref="FilePath"/>.
        /// Returns an empty string if the provided <paramref name="value"/> is null/whitespace (after normalization).
        /// </returns>
        /// <remarks>
        /// - The method ensures the target directory exists by calling <see cref="Directory.CreateDirectory(string)"/>.
        /// - Directory creation and underlying I/O exceptions (for example, <see cref="UnauthorizedAccessException"/>, 
        ///   <see cref="IOException"/>) will bubble up to the caller.
        /// - The method attempts to normalize inputs and will not throw for common path formatting issues; however,
        ///   filesystem operations (creating directories) may throw.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Not thrown by this method; inputs are tolerated and normalized. Filesystem APIs may throw exceptions described above.</exception>
        /// <example>
        /// <code>
        /// // Ensures "%TEMP%\MyApp" exists and returns the full path to "log.txt" inside it
        /// var path = File_Utils.SetFile("log.txt", "%TEMP%\\MyApp");
        /// // e.g. "C:\Users\you\AppData\Local\Temp\MyApp\log.txt"
        /// </code>
        /// </example>
        public static string SetFile(string value, string ConfiguredFolder)
        {
            string targetPath;
            if (!string.IsNullOrWhiteSpace(ConfiguredFolder))
            {
                var folder = NormalizePath(ConfiguredFolder);
                Directory.CreateDirectory(folder);
                targetPath = NormalizePath(Path.Combine(folder, value));
            }
            else
            {
                targetPath = NormalizePath(value);
            }

            FilePath = targetPath;
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? ".");

            return FilePath;
        }

        /// <summary>
        /// Normalizes a filesystem path:
        /// - returns empty string for null/whitespace input,
        /// - expands environment variables,
        /// - trims surrounding quotes and whitespace,
        /// - converts alternate directory separators to the platform default,
        /// - attempts to resolve to a full path,
        /// - removes trailing separators unless the path is a drive/root.
        /// </summary>
        /// <param name="path">Input path to normalize.</param>
        /// <returns>Normalized path string, or empty string when <paramref name="path"/> is null/whitespace.</returns>
        /// <remarks>
        /// - This method is resilient: calls to <see cref="Path.GetFullPath(string)"/> and other path APIs are wrapped in try/catch
        ///   and, on failure, the method returns the best-effort normalized string rather than throwing.
        /// - When provided a relative path the method will attempt to resolve it to an absolute path. If resolution fails,
        ///   it will return the expanded, separator-normalized input.
        /// - Trailing directory separators are removed except when the path represents a drive/root (for example "C:\").
        /// </remarks>
        /// <example>
        /// <code>
        /// var normalized = File_Utils.NormalizePath("  \"%TEMP%/mydir/\" ");
        /// // returns a platform-correct absolute path without trailing separator
        /// </code>
        /// </example>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            // Expand environment variables and trim quotes/whitespace
            var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));

            // Use OS-specific directory separator
            expanded = expanded.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            // Resolve to full path when possible
            try
            {
                expanded = Path.GetFullPath(expanded);
            }
            catch
            {
                // If invalid, keep the expanded value as-is
            }

            // Remove trailing separators unless path is a root
            try
            {
                var root = Path.GetPathRoot(expanded);
                if (!string.IsNullOrEmpty(expanded) && !string.IsNullOrEmpty(root) && !string.Equals(expanded, root, StringComparison.OrdinalIgnoreCase))
                {
                    expanded = expanded.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
            }
            catch
            {
                // Ignore trimming if path APIs throw
            }

            return expanded;
        }
    }
}
