using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Girvs.Domain.Caching.ComponentModel
{
    /// <summary>
    /// 通用字典类型已转换
    /// </summary>
    /// <typeparam name="K">Key type (simple)</typeparam>
    /// <typeparam name="V">Value type (simple)</typeparam>
    public class GenericDictionaryTypeConverter<K, V> : TypeConverter
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
            TypeConverterKey = TypeDescriptor.GetConverter(typeof(K));
            if (TypeConverterKey == null)
                throw new InvalidOperationException("No type converter exists for type " + typeof(K).FullName);
            TypeConverterValue = TypeDescriptor.GetConverter(typeof(V));
            if (TypeConverterValue == null)
                throw new InvalidOperationException("No type converter exists for type " + typeof(V).FullName);
        }

        /// <summary>
        /// 获取一个值，该值指示此转换器是否可以将给定源类型的对象转换为转换器的本机类型使用上下文。
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
        /// 将给定的对象转换为转换器的本机类型。
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
            var items = string.IsNullOrEmpty(input) ? new string[0] : input.Split(';').Select(x => x.Trim()).ToArray();

            var result = new Dictionary<K, V>();
            Array.ForEach(items, s =>
            {
                var keyValueStr = string.IsNullOrEmpty(s) ? new string[0] : s.Split(',').Select(x => x.Trim()).ToArray();
                if (keyValueStr.Length != 2)
                    return;

                object dictionaryKey = (K)TypeConverterKey.ConvertFromInvariantString(keyValueStr[0]);
                object dictionaryValue = (V)TypeConverterValue.ConvertFromInvariantString(keyValueStr[1]);
                if (dictionaryKey == null || dictionaryValue == null)
                    return;

                if (!result.ContainsKey((K)dictionaryKey))
                    result.Add((K)dictionaryKey, (V)dictionaryValue);
            });

            return result;
        }

        /// <summary>
        /// 使用指定的上下文和参数将给定值对象转换为指定的目标类型
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
            var dictionary = (IDictionary<K, V>)value;
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
}