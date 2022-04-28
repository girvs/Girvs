using System;
using System.Collections.Generic;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Dto;

namespace Girvs.AuthorizePermission
{
    public class AuthorizePermissionModel : IDto
    {
        public AuthorizePermissionModel()
        {
            Permissions = new Dictionary<string, string>();
            OperationPermissionModels = new List<OperationPermissionModel>();
        }

        public string ServiceName { get; set; }
        public Guid ServiceId { get; set; }

        public List<OperationPermissionModel> OperationPermissionModels { get; set; }
        public Dictionary<string, string> Permissions { get; set; }
        
        /// <summary>
        /// 所属标签
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Order { get; set; }


        /// <summary>
        /// 所属的子系统模块
        /// </summary>
        public FuncModule FuncModule { get; set; }

        /// <summary>
        /// 其它相关参数
        /// </summary>
        public string[] OtherParams { get; set; }
    }


    public class OperationPermissionModel : IDto
    {
        public string OperationName { get; set; }
        public Permission Permission { get; set; }
        public UserType UserType { get; set; }

        /// <summary>
        /// 所属的子系统模块
        /// </summary>
        public FuncModule FuncModule { get; set; }

        /// <summary>
        /// 其它相关参数
        /// </summary>
        public string[] OtherParams { get; set; }
    }
}