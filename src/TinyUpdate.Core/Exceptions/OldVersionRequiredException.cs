using System;

namespace TinyUpdate.Core.Exceptions
{
    public class OldVersionRequiredException : Exception
    {
        public override string Message => "We require the old version in a delta update";
    }
}