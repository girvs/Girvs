using System;
using Girvs.AuthorizePermission.Enumerations;

namespace Girvs.AuthorizePermission
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class DataRuleAttribute : Attribute
    {
        public DataRuleAttribute(string attributeDesc, UserType userType = UserType.All)
        {
            AttributeDesc = attributeDesc;
            UserType = userType;
        }

        /// <summary>
        /// 标记说明
        /// </summary>
        public string AttributeDesc { get; private set; }

        public UserType UserType { get; private set; }
    }
}