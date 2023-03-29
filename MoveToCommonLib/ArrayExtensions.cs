using System;
using Vintagestory.API.Util;

namespace StoneQuarry
{
    [Obsolete]
    public static class ArrayExtensions
    {
        /// <summary>
        /// Creates a new copy of the array with value appened to the end of the array
        /// if condition is true
        /// </summary>
        public static T[] AppendIf<T>(this T[] array, bool condition, T value)
        {
            if (condition)
            {
                return array.Append(value);
            }

            return array;
        }

        /// <summary>
        /// Creates a new copy of the array with value appened to the end of the array
        /// if condition is true
        /// </summary>
        public static T[] AppendIf<T>(this T[] array, bool condition, params T[] value)
        {
            if (condition)
            {
                return array.Append(value);
            }

            return array;
        }
    }
}
