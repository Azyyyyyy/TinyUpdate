using System;
using System.Linq;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Extensions
{
    public static class ILoggingExt
    {
        public static string? GetPropertyDetails(this ILogging loggingExt, object?[] propertyValues)
        {
            if (!propertyValues.Any())
            {
                return null;
            }

            var s = Environment.NewLine + "Values" + Environment.NewLine + "------" + Environment.NewLine;
            for (var i = 0; i < propertyValues.LongLength; i++)
            {
                s += $"Val {i}: {{{i}}}" + Environment.NewLine;
            }
            return s.TrimEnd();
        }
    }
}