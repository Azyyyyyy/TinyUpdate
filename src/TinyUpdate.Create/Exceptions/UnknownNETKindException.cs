using System;

namespace TinyUpdate.Create.Exceptions
{
    public class UnknownNETKindException : Exception
    {
        public override string Message => "We don't know/support this version of .NET";
    }
}