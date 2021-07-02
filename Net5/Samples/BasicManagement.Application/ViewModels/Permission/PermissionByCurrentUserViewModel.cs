using System;
using System.Collections.Generic;
using Girvs.AuthorizePermission.Enumerations;

namespace BasicManagement.Application.ViewModels.Permission
{
    public class PermissionByCurrentUserViewModel
    {
        public PermissionByCurrentUserViewModel()
        {
            PermissionStr = new List<string>();
        }

        /// <summary>
        /// 对应功能模块的ID
        /// </summary>
        public Guid AppliedObjectID { get; set; }

        /// <summary>
        /// 权限分类，方便扩展
        /// </summary>
        public PermissionValidateObjectType ValidateObjectType { get; set; }

        public List<string> PermissionStr { get; set; }
    }
}