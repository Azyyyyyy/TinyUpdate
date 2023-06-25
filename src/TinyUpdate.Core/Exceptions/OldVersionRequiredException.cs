using System;

namespace TinyUpdate.Core.Exceptions;

/// <summary>
/// <see cref="Exception"/> for when we haven't been given the old version but trying to do a delta update
/// </summary>
public class OldVersionRequiredException : Exception
{
    public override string Message => "We require the old version in a delta update";
}