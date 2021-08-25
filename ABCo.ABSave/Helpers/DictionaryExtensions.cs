using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Helpers
{
    // Adds a basic implementation of "GetValueOrDefault" to dictionaries on Standard 2.0 for consistency.
#if NETSTANDARD2_0
    internal static class DictionaryExtensions
    {
        public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
            where TValue : class
        {
            if (dict.TryGetValue(key, out TValue val))
                return val;

            return default;
        }
    }
#endif
}
