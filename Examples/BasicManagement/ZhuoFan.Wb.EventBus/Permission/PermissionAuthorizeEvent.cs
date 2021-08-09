using System;
using System.Collections.Generic;
using Girvs.EventBus;
using Users;

namespace Power.EventBus.Permission
{
    public class PermissionAuthorize
    {
        public PermissionAuthorize()
        {
            Permissions = new Dictionary<string, string>();
        }

        public string ServiceName { get; set; }
        public Guid ServiceId { get; set; }
        public Dictionary<string, string> Permissions { get; set; }
    }


    public class PermissionAuthorizeEvent : IntegrationEvent
    {
        public List<PermissionAuthorize> PermissionAuthorizes { get; set; } = new List<PermissionAuthorize>();
        public List<AuthorizeUserRule> AuthorizeUserRules { get; set; } = new List<AuthorizeUserRule>();
    }

    public class AuthorizeUserRule
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; set; }

        /// <summary>
        /// 字段对应的数据来源（即接口名称）
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 字段的说明
        /// </summary>
        public string FieldDesc { get; set; }
    }
}