namespace Girvs.Cache.ComponentModel;

/// <summary>
/// Generic Dictionary type converted
/// </summary>
/// <typeparam name="TK">Key type (simple)</typeparam>
/// <typeparam name="TV">Value type (simple)</typeparam>
public class GenericDictionaryTypeConverter<TK, TV> : TypeConverter
{
    /// <summary>
    /// Type converter
    /// </summary>
    protected readonly TypeConverter TypeConverterKey;

    /// <summary>
    /// Type converter
    /// </summary>
    protected readonly TypeConverter TypeConverterValue;

    public GenericDictionaryTypeConverter()
    {
        TypeConverterKey = TypeDescriptor.GetConverter(typeof(TK));
        if (TypeConverterKey == null)
            throw new InvalidOperationException("No type converter exists for type " + typeof(TK).FullName);
        TypeConverterValue = TypeDescriptor.GetConverter(typeof(TV));
        if (TypeConverterValue == null)
            throw new InvalidOperationException("No type converter exists for type " + typeof(TV).FullName);
    }

    /// <summary>
    /// Gets a value indicating whether this converter can        
    /// convert an object in the given source type to the native type of the converter
    /// using the context.
    /// </summary>
    /// <param name="context">Context</param>
    /// <param name="sourceType">Source type</param>
    /// <returns>Result</returns>
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
            return true;

        return base.CanConvertFrom(context, sourceType);
    }

    /// <summary>
    /// Converts the given object to the converter's native type.
    /// </summary>
    /// <param name="context">Context</param>
    /// <param name="culture">Culture</param>
    /// <param name="value">Value</param>
    /// <returns>Result</returns>
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (!(value is string))
            return base.ConvertFrom(context, culture, value);

        var input = (string)value;
        var items = string.IsNullOrEmpty(input) ? Array.Empty<string>() : input.Split(';').Select(x => x.Trim()).ToArray();

        var result = new Dictionary<TK, TV>();
        Array.ForEach(items, s =>
        {
            var keyValueStr = string.IsNullOrEmpty(s) ? Array.Empty<string>() : s.Split(',').Select(x => x.Trim()).ToArray();
            if (keyValueStr.Length != 2)
                return;

            object dictionaryKey = (TK)TypeConverterKey.ConvertFromInvariantString(keyValueStr[0]);
            object dictionaryValue = (TV)TypeConverterValue.ConvertFromInvariantString(keyValueStr[1]);
            if (dictionaryKey == null || dictionaryValue == null)
                return;

            if (!result.ContainsKey((TK)dictionaryKey))
                result.Add((TK)dictionaryKey, (TV)dictionaryValue);
        });

        return result;
    }

    /// <summary>
    /// Converts the given value object to the specified destination type using the specified context and arguments
    /// </summary>
    /// <param name="context">Context</param>
    /// <param name="culture">Culture</param>
    /// <param name="value">Value</param>
    /// <param name="destinationType">Destination type</param>
    /// <returns>Result</returns>
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType != typeof(string))
            return base.ConvertTo(context, culture, value, destinationType);

        var result = string.Empty;
        if (value == null)
            return result;

        //we don't use string.Join() because it doesn't support invariant culture
        var counter = 0;
        var dictionary = (IDictionary<TK, TV>)value;
        foreach (var keyValue in dictionary)
        {
            result += $"{Convert.ToString(keyValue.Key, CultureInfo.InvariantCulture)}, {Convert.ToString(keyValue.Value, CultureInfo.InvariantCulture)}";
            //don't add ; after the last element
            if (counter != dictionary.Count - 1)
                result += ";";
            counter++;
        }

        return result;
    }
}