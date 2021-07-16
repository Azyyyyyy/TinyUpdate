using System;

namespace TinyUpdate.Core.Extensions
{
    public static class DateTimeExt
    {
        public static string ToFileName(this DateTime time) => time.ToString("d-M-yyyy_h-mm-ss");
    }
}
