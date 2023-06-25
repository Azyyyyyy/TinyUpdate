using System;

namespace TinyUpdate.Core.Exceptions;

/// <summary>
/// <see cref="Exception"/> for when we been given an invalid hash
/// </summary>
public class InvalidHashException : Exception
{
    public override string Message => "We have been given an invalid hash!";
}