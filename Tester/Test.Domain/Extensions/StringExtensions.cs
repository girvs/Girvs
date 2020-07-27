using System;

namespace Test.Domain.Extensions
{
    public static class StringExtensions
    {
        public static Guid ToGuid(this string str)
        {
            return Guid.Parse(str);
        }
    }
}