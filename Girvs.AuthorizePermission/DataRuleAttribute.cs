using System;

namespace Girvs.AuthorizePermission
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class DataRuleAttribute : Attribute
    {
        public DataRuleAttribute(string attributeDesc)
        {
            AttributeDesc = attributeDesc;
        }

        /// <summary>
        /// 标记说明
        /// </summary>
        public string AttributeDesc { get; private set; }
    }
}