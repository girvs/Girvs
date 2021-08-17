using System;

namespace Girvs
{
    public class GirvsConvert
    {
        public static T ToSpecifiedType<T>(object value)
        {
            return (T)ToSpecifiedType(typeof(T).FullName, value);
        }


        public static object ToSpecifiedType(string typeStr, object value)
        {
            if (value is null)
            {
                throw new GirvsException(568, $"{value}要转换的值不能为空");
            }

            if (typeStr == "System.Int32")
            {
                return Convert.ToInt32(value);
            }

            if (typeStr == "System.Int16")
            {
                return Convert.ToInt16(value);
            }

            if (typeStr == "System.Int64")
            {
                return Convert.ToInt64(value);
            }

            if (typeStr == "System.Boolean")
            {
                return Convert.ToBoolean(value);
            }

            if (typeStr == "System.Byte")
            {
                return Convert.ToByte(value);
            }

            if (typeStr == "System.Char")
            {
                return Convert.ToChar(value);
            }

            if (typeStr == "System.Decimal")
            {
                return Convert.ToDecimal(value);
            }

            if (typeStr == "System.Double")
            {
                return Convert.ToDouble(value);
            }

            if (typeStr == "System.Single")
            {
                return Convert.ToSingle(value);
            }

            if (typeStr == "System.DateTime")
            {
                return Convert.ToDateTime(value);
            }

            if (typeStr == "System.Guid")
            {
                var guidStr = value.ToString();
                return string.IsNullOrEmpty(guidStr) ? Guid.Empty : Guid.Parse(guidStr);
            }


            throw new GirvsException(568, $"未找到指定类型：{typeStr}的转换器");
        }
    }
}