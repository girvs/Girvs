namespace Girvs.Extensions;

public static class DictionaryExtensions
{
    [CanBeNull]
    public static TValue GetDictionaryValueByKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : class
    {
        return dictionary.ContainsKey(key) ? dictionary[key] : null;
    }


    public static void SetDictionaryKeyValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
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