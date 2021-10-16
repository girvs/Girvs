using System.Linq.Expressions;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.ServiceDataRule
{
    public class CreateOrUpdateServiceDataRuleCommand : Command
    {
        public CreateOrUpdateServiceDataRuleCommand(string entityTypeName, string entityDesc, string fieldName,
            string fieldDesc, string fieldType, string fieldValue, ExpressionType expressionType, UserType userType)
        {
            EntityTypeName = entityTypeName;
            EntityDesc = entityDesc;
            FieldName = fieldName;
            FieldDesc = fieldDesc;
            FieldType = fieldType;
            FieldValue = fieldValue;
            ExpressionType = expressionType;
            UserType = userType;
        }

        public override string CommandDesc { get; set; } = "添加服务数据规则授权";

        /// <summary>
        /// 实体说明
        /// </summary>
        public string EntityDesc { get; private set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string EntityTypeName { get; private set; }

        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// 字段的说明
        /// </summary>
        public string FieldDesc { get; private set; }

        /// <summary>
        /// 字段类型（预留）
        /// </summary>
        public string FieldType { get; private set; }

        /// <summary>
        /// 字段赋值
        /// </summary>
        public string FieldValue { get; private set; }

        /// <summary>
        /// 表达式运算符
        /// </summary>
        public ExpressionType ExpressionType { get; private set; }
        
        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; private set; }
    }
}