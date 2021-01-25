using System;

namespace Girvs.Domain.Managers
{
    /// <summary>
    /// 主要作为查询Key的关键字
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class GirvsHttpClientConfigAttribute : Attribute
    {
        public string ClientName { get; }

        public GirvsHttpClientConfigAttribute(string clientName)
        {
            ClientName = clientName;
        }
    }
}