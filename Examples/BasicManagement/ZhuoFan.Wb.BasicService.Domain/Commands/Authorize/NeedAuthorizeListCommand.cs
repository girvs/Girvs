using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.Authorize
{
    public class NeedAuthorizeListCommand : Command
    {
        public NeedAuthorizeListCommand(ICollection<ServicePermissionCommandModel> servicePermissionCommandModels, ICollection<ServiceDataRuleCommandModel> serviceDataRuleCommandModels)
        {
            ServicePermissionCommandModels = servicePermissionCommandModels;
            ServiceDataRuleCommandModels = serviceDataRuleCommandModels;
        }
        public override string CommandDesc { get; set; } = "初始化服务需要授权列表";

        public ICollection<ServicePermissionCommandModel> ServicePermissionCommandModels { get; private set; }
        public ICollection<ServiceDataRuleCommandModel> ServiceDataRuleCommandModels { get; private set; }
    }


    public class ServicePermissionCommandModel
    {
        public string ServiceName { get; set; }
        public Guid ServiceId { get; set; }
        public Dictionary<string, string> Permissions { get; set; }
        public List<OperationPermissionModel> OperationPermissionModels { get; set; }
    }


    public class ServiceDataRuleCommandModel
    {
        /// <summary>
        /// 实体说明
        /// </summary>
        public string EntityDesc { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string EntityTypeName { get; set; }

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

        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; set; }
    }
}