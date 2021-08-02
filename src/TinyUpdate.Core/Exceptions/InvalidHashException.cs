using System;

namespace TinyUpdate.Core.Exceptions
{
    public class InvalidHashException : Exception
    {
        public override string Message => "We have been given an invalid hash!";
    }
}