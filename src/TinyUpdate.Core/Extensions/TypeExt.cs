using System;

namespace TinyUpdate.Core.Extensions;

public static class TypeExt
{
    public static bool IsNumber(this Type type)
    {
        return type == typeof(sbyte)
               || type == typeof(byte)
               || type == typeof(short)
               || type == typeof(ushort)
               || type == typeof(int)
               || type == typeof(uint)
               || type == typeof(long)
               || type == typeof(ulong)
               || type == typeof(nint)
               || type == typeof(nuint)
               || type == typeof(float)
               || type == typeof(double)
               || type == typeof(decimal);
    }
}