using System;

namespace TinyUpdate.Core.Exceptions
{
    public class BadFilesizeException : Exception
    {
        public override string Message => "Filesize can't be under 0 bytes!";
    }
}