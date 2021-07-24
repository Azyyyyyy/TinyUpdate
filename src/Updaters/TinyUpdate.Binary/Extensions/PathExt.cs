using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        /// This returns the folder that the application should be in based on the version
        /// </summary>
        [return: NotNullIfNotNull("version")]
        public static string? GetApplicationFolder(this Version? version) =>
            version == null ? null : $"app-{version.ToString(4)}";

        /// <summary>
        /// Removes path from strings
        /// </summary>
        /// <param name="enumerable">file paths that contains the path</param>
        /// <param name="path">Path to remove</param>
        /// <returns>file paths without <see cref="path"/></returns>
        public static IEnumerable<string> RemovePath(this IEnumerable<string> enumerable, string path) =>
            enumerable.Select(file => file.RemovePath(path));

        /// <summary>
        /// Removes path from string
        /// </summary>
        /// <param name="str">file that contains the path</param>
        /// <param name="path">Path to remove</param>
        /// <returns>file path without <see cref="path"/></returns>
        public static string RemovePath(this string str, string path)
        {
            return str[(path.Length + 1)..];
        }
        
        /// <summary>
        /// Gets the relative path for two files
        /// </summary>
        /// <param name="baseFile">First file to base the path on</param>
        /// <param name="newFile">Second file to base the path on</param>
        public static string GetRelativePath(this string baseFile, string newFile)
        {
            //If the pathing is completely the same then just grab the filename ¯\_(ツ)_/¯
            if (baseFile == newFile)
            {
                return Path.GetFileName(newFile);
            }
            
            var basePath = baseFile;

            var count = Math.Min(baseFile.Length, newFile.Length);
            if (baseFile.Length > newFile.Length)
            {
                baseFile = baseFile[^newFile.Length..];
            }
            else
            {
                newFile = newFile[^baseFile.Length..];
            }

            var index = count - 1;
            for (;0 <= index; index--)
            {
                if (baseFile[index] != newFile[index])
                {
                    break;
                }
            }
            Debug.Assert(index != -1, "We tried to compare two strings that are the same!");
            
            basePath = basePath[index..];
            basePath = basePath[0] != Path.DirectorySeparatorChar ? 
                basePath[(basePath.IndexOf(Path.DirectorySeparatorChar) + 1)..] : basePath[1..];

            return basePath;
        }

        /// <summary>
        /// This checks if the folder exists, deleting and recreating the folder if it does
        /// </summary>
        /// <param name="folder">Folder to check</param>
        public static void RemakeFolder(this string folder)
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