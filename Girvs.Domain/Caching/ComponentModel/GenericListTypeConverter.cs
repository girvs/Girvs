using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Girvs.Domain.Caching.ComponentModel
{
    /// <summary>
    /// 通用列表类型已转换
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class GenericListTypeConverter<T> : TypeConverter
    {
        /// <summary>
        /// Type converter
        /// </summary>
        protected readonly TypeConverter TypeConverter;

        public GenericListTypeConverter()
        {
            TypeConverter = TypeDescriptor.GetConverter(typeof(T));
            if (TypeConverter == null)
                throw new InvalidOperationException("No type converter exists for type " + typeof(T).FullName);
        }

        /// <summary>
        /// 从逗号分隔的字符串中获取字符串数组
        /// </summary>
        /// <param name="input">Input</param>
        /// <returns>Array</returns>
        protected virtual string[] GetStringArray(string input)
        {
            return string.IsNullOrEmpty(input) ? new string[0] : input.Split(',').Select(x => x.Trim()).ToArray();
        }

        /// <summary>
        /// 获取一个值，该值指示此转换器是否可以
        /// 将给定源类型的对象转换为转换器的本机类型
        /// 使用上下文。
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="sourceType">Source type</param>
        /// <returns>Result</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType != typeof(string))
                return base.CanConvertFrom(context, sourceType);

            var items = GetStringArray(sourceType.ToString());
            return items.Any();
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
            if (!(value is string) && value != null)
                return base.ConvertFrom(context, culture, value);

            var items = GetStringArray((string)value);
            var result = new List<T>();
            Array.ForEach(items, s =>
            {
                var item = TypeConverter.ConvertFromInvariantString(s);
                if (item != null)
                {
                    result.Add((T)item);
                }
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
            for (var i = 0; i < ((IList<T>)value).Count; i++)
            {
                var str1 = Convert.ToString(((IList<T>)value)[i], CultureInfo.InvariantCulture);
                result += str1;
                //don't add comma after the last element
                if (i != ((IList<T>)value).Count - 1)
                    result += ",";
            }

            return result;
        }
    }
}