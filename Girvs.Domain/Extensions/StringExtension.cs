using System;
using System.Security.Cryptography;
using System.Text;

namespace Girvs.Domain.Extensions
{
    public static class StringExtension
    {
        public static string ToMd5(this string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            //32位大写
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
                var strResult = BitConverter.ToString(result);
                string result3 = strResult.Replace("-", "");
                return result3;
            }
        }

        public static string ConverterInitialsLowerCase(this string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            var initialStr = str.Substring(0, 1);
            var leftOver = str.Substring(1);

            return $"{initialStr.ToLower()}{leftOver}";
        }
    }
}
