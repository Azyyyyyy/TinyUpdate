using System;

namespace TinyUpdate.Core.Exceptions
{
    /// <summary>
    /// <see cref="Exception"/> for when we are given a file path that can't be used
    /// </summary>
    public class InvalidFilePathException : Exception
    {
        public InvalidFilePathException(char? invalidChar) 
            : base(!invalidChar.HasValue || char.IsWhiteSpace(invalidChar.Value) ?
                "We wasn't given anything to make a filename off" :
                $"filename given contains a char that is not allowed in a file path (Invalid char: {invalidChar})")
        { }
    }
}