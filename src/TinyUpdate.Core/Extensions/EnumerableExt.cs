using System;
using System.Collections.Generic;

namespace TinyUpdate.Core.Extensions
{
    /// <summary>
    /// Extensions for any <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class EnumerableExt
    {
        /// <summary>
        /// Gets the index of a item
        /// </summary>
        /// <param name="enumerable"><see cref="IEnumerable{T}"/> to check</param>
        /// <param name="action">Action to see if this is the item</param>
        /// <typeparam name="T">item type</typeparam>
        /// <returns>Index of item or -1 if not found</returns>
        public static int IndexOf<T>(this IEnumerable<T?> enumerable, Func<T?, bool> action)
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