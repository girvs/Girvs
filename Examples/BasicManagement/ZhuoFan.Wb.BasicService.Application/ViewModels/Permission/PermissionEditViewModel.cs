using System;
using System.Collections.Generic;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Dto;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.Permission
{
    public class PermissionEditViewModel : IDto
    {
        public PermissionEditViewModel()
        {
            PermissionStr = new List<string>();
        }

        /// <summary>
        /// 对应用户ID或角色ID
        /// </summary>
        public Guid AppliedID { get; set; }

        /// <summary>
        /// 对应功能模块的ID
        /// </summary>
        public Guid AppliedObjectID { get; set; }

        /// <summary>
        /// 指定对角色授权,还是用户授权(0为角色 1为用户)
        /// </summary>
        public PermissionAppliedObjectType AppliedObjectType { get; set; }

        /// <summary>
        /// 权限分类，方便扩展
        /// </summary>
        public PermissionValidateObjectType ValidateObjectType { get; set; }

        public List<string> PermissionStr { get; set; }
    }

    public class SavePermisssionEditViewModel
    {
        public SavePermisssionEditViewModel()
        {
            Ps = new List<PermissionEditViewModel>();
        }

        /// <summary>
        /// 对应用户ID或角色ID
        /// </summary>
        public Guid AppliedId { get; set; }

        /// <summary>
        /// 指定对角色授权,还是用户授权(0为角色 1为用户)
        /// </summary>
        public PermissionAppliedObjectType AppliedObjectType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<PermissionEditViewModel> Ps { get; set; }
    }
}