using System.Collections.Generic;
using System.IO;
using TinyUpdate.Core.Logger;

namespace TinyUpdate.Core.Utils
{
    /// <summary>
    /// Functions to assist in making sure that what is passed for files are valid
    /// </summary>
    public static class FilePathUtil
    {
        private static readonly ILogging Logger = Logging.CreateLogger("FilePathUtil");
        private static readonly string[] FileNameInvalidChars;
        private static readonly string[] PathInvalidChars;

        //TODO: Maybe make a char Contains extension so we don't need this?
        //We have to make it a string because string.Contains() doesn't allow checking with chars....
        static FilePathUtil()
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            FileNameInvalidChars = new string[invalidChars.LongLength];
            for (var i = 0; i < invalidChars.LongLength; i++)
            {
                FileNameInvalidChars[i] = invalidChars[i].ToString();
            }
            
            invalidChars = Path.GetInvalidPathChars();
            PathInvalidChars = new string[invalidChars.LongLength];
            for (var i = 0; i < invalidChars.LongLength; i++)
            {
                PathInvalidChars[i] = invalidChars[i].ToString();
            }
        }

        /// <summary>
        /// Gets if the string contains any char that is not allowed in a file path
        /// </summary>
        /// <param name="s">File name to check</param>
        /// <param name="invalidChar"><see cref="char"/> that is invalid</param>
        public static bool IsValidForFileName(this string s, out char? invalidChar) => CheckValidation(FileNameInvalidChars, s, out invalidChar);
        
        /// <summary>
        /// Gets if the string contains any char that is not allowed in a file name
        /// </summary>
        /// <param name="s">File path to check</param>
        /// <param name="invalidChar"><see cref="char"/> that is invalid</param>
        public static bool IsValidForFilePath(this string s, out char? invalidChar) => CheckValidation(PathInvalidChars, s, out invalidChar);
        
        
        private static bool CheckValidation(IEnumerable<string> chars, string s, out char? invalidChar)
        {
            Logger.Debug("Checking {0}", s);
            //Check that the string even has anything
            invalidChar = null;
            if (string.IsNullOrWhiteSpace(s))
            {
                Logger.Debug("{0} is not useable", s);
                return false;
            }

            //Check the chars
            foreach (var charToCheck in chars)
            {
                if (s.Contains(charToCheck))
                {
                    invalidChar = charToCheck[0];
                    Logger.Debug("{0} is not useable", s);
                    return false;
                }
            }

            Logger.Debug("{0} is useable", s);
            return true;
        }
    }
}