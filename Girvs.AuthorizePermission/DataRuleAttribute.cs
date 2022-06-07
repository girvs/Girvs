using System;
using Girvs.AuthorizePermission.Enumerations;

namespace Girvs.AuthorizePermission
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class DataRuleAttribute : Attribute
    {
        public DataRuleAttribute(
            string attributeDesc, UserType userType = UserType.All,
            string tag = "",
            int order = 0,
            ConditionType conditionType = ConditionType.Or
        )
        {
            AttributeDesc = attributeDesc;
            ConditionType = conditionType;
            Tag = tag;
            Order = order;
            UserType = userType;
        }

        /// <summary>
        /// 标记说明
        /// </summary>
        public string AttributeDesc { get; private set; }

        public UserType UserType { get; private set; }

        /// <summary>
        /// 所属标签
        /// </summary>
        public string Tag { get; private set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// 条件类型
        /// </summary>
        public ConditionType ConditionType { get; private set; }
    }

    public enum ConditionType
    {
        And,
        Or
    }
}