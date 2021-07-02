using System;
using System.Collections.Generic;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Dto;

namespace BasicManagement.Application.ViewModels.Permission
{
    public class PermissionDetailViewModel : IDto
    {
        public PermissionDetailViewModel()
        {
            PermissionStr = new List<string>();
        }
        public Guid AppliedID { get; set; }

        /// <summary>
        /// 对应功能模块的ID
        /// </summary>
        public Guid AppliedObjectID { get; set; }

        /// <summary>
        /// 对应授权的类型
        /// </summary>
        public PermissionAppliedObjectType AppliedObjectType { get; set; }

        /// <summary>
        /// 权限分类，方便扩展
        /// </summary>
        public PermissionValidateObjectType ValidateObjectType { get; set; }

        public List<string> PermissionStr { get; set; }
    }
}