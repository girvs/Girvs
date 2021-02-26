using System;

namespace Power.BasicManagement.Domain.Extensions
{
    public static class StringExtensions
    {
        public static Guid ToGuid(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return Guid.Empty;
            }
            return Guid.Parse(str);
        }
        
        public static Guid? ToHasGuid(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return (Guid?)null;
            }
            return Guid.Parse(str);
        }
    }
}