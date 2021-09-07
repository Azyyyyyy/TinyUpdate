using System;

namespace TinyUpdate.Core.Exceptions
{
    /// <summary>
    /// <see cref="Exception"/> for when a filesize is somehow under 0 bytes
    /// </summary>
    public class BadFilesizeException : Exception
    {
        public override string Message => "Filesize can't be under 0 bytes!";
    }
}