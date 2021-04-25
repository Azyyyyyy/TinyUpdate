using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Binary.Extensions
{
    /// <summary>
    /// Extensions to make working with paths easier
    /// </summary>
    public static class PathExt
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(PathExt));

        /// <summary>
        /// Removes path from string
        /// </summary>
        /// <param name="enumerable">file paths that contain the path</param>
        /// <param name="path">Path to remove</param>
        /// <returns>file paths without <see cref="path"/></returns>
        public static IEnumerable<string> RemovePath(this IEnumerable<string> enumerable, string path)
        {
            return enumerable.Select(file =>
                file.Substring(path.Length + 1));
        }

        /// <summary>
        /// Gets the relative path for two files
        /// </summary>
        /// <param name="baseFile">First file to base the path on</param>
        /// <param name="newFile">Second file to base the path on</param>
        public static string GetRelativePath(this string baseFile, string newFile)
        {
            var basePath = baseFile;
            var newPath = newFile;
            while (!newPath.Contains(basePath))
            {
                basePath = basePath.Substring(basePath.IndexOf(Path.DirectorySeparatorChar) + 1);
            }

            newPath = newFile.Replace(basePath, "");
            newPath = newFile.Substring(newPath.Length);
            return newPath;
        }

        /// <summary>
        /// This checks if this folder exists and if it does, it will delete it and recreate it
        /// </summary>
        /// <param name="folder">Folder to check for</param>
        public static void CheckForFolder(this string folder)
        {
            /*Create the folder that's going to contain this update
             deleting the folder if it already exists*/
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);
        }

        /// <summary>
        /// Creates a <see cref="Directory"/> from two paths
        /// </summary>
        /// <param name="basePath">Base path</param>
        /// <param name="folderPath">path to append to <see cref="basePath"/></param>
        public static void CreateDirectory(this string basePath, string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            var folder = Path.Combine(basePath, folderPath);
            Logger.Debug("Creating folder {0}", folder);
            Directory.CreateDirectory(folder);
        }
    }
}