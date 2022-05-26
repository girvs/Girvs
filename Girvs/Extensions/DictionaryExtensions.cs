using System.Collections.Generic;
using JetBrains.Annotations;

namespace Girvs.Extensions
{
    public static class DictionaryExtensions
    {
        [CanBeNull]
        public static TValue GetDictionaryValueByKey<Tkey, TValue>(this IDictionary<Tkey, TValue> dictionary, Tkey key)
            where TValue : class
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : null;
        }


        public static void SetDictionaryKeyValue<Tkey, TValue>(this IDictionary<Tkey, TValue> dictionary, Tkey key,
            TValue value)
            where TValue : class
        {
            if (value == null)
            {
                return;
            }
            
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }
    }
}