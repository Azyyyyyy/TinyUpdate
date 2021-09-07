using System;

namespace TinyUpdate.Core.Exceptions
{
    /// <summary>
    /// <see cref="Exception"/> for when the file name that's been given, can't be used
    /// </summary>
    public class InvalidFileNameException : Exception
    {
        public InvalidFileNameException(char? invalidChar)
            : base(!invalidChar.HasValue || char.IsWhiteSpace(invalidChar.Value)
                ? "We wasn't given anything to make a filename off"
                : $"filename given contains a char that is not allowed (Invalid char: {invalidChar})")
        { }
    }
}