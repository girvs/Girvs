using System.Collections.Generic;
using System.Linq.Expressions;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Dto;

namespace Girvs.AuthorizePermission
{
    /// <summary>
    /// 数据规则授权模型
    /// </summary>
    public class AuthorizeDataRuleModel : IDto
    {
        public AuthorizeDataRuleModel()
        {
            AuthorizeDataRuleFieldModels = new List<AuthorizeDataRuleFieldModel>();
        }

        /// <summary>
        /// 实体说明
        /// </summary>
        public string EntityDesc { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// 所属标签
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 实体需要相关授权的字段列表
        /// </summary>
        public List<AuthorizeDataRuleFieldModel> AuthorizeDataRuleFieldModels { get; set; }
    }


    public class AuthorizeDataRuleFieldModel : IDto
    {
        public UserType UserType { get; set; }
        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 字段的说明
        /// </summary>
        public string FieldDesc { get; set; }

        /// <summary>
        /// 字段类型（预留）
        /// </summary>
        public string FieldType { get; set; }

        /// <summary>
        /// 字段赋值
        /// </summary>
        public string FieldValue { get; set; }

        /// <summary>
        /// 表达式运算符
        /// </summary>
        public ExpressionType ExpressionType { get; set; }
    }
}