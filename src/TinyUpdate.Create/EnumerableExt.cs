using System;
using System.Collections.Generic;

namespace TinyUpdate.Create
{
    public static class EnumerableExt
    {
        public static int IndexOf<T>(this IEnumerable<T> enumerable, Func<T, bool> action)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (action.Invoke(item))
                {
                    return index;
                }
                index++;
            }

            return -1;
        }
    }
}