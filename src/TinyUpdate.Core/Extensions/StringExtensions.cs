using System;
using System.Runtime.CompilerServices;

namespace TinyUpdate.Core.Extensions;

public static class StringExtensions
{
    public static bool IsNewChars(this string message)
    {
        var messageSpan = message.AsSpan().Trim();
        return messageSpan.Length is <= 2 and > 0 && ContainsNewChars(messageSpan[0]);
    }

#if NETSTANDARD2_1_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithNewChars(this string message) => message.EndsWith('\r') || message.EndsWith('\n');
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithNewChars(this string message) => message.EndsWith("\r") || message.EndsWith("\n");
#endif
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsNewChars(this char message) => message is '\r' or '\n';
}