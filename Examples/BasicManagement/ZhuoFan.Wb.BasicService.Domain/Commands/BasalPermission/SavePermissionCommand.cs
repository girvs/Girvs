using System;
using System.Collections.Generic;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.BasalPermission
{
    public class SavePermissionCommand : Command
    {
        public SavePermissionCommand(Guid appliedId, PermissionAppliedObjectType appliedObjectType,
            PermissionValidateObjectType validateObjectType, IList<ObjectPermission> objectIdPermissions)
        {
            AppliedID = appliedId;
            AppliedObjectType = appliedObjectType;
            ValidateObjectType = validateObjectType;
            ObjectPermissions = objectIdPermissions;
        }

        public override string CommandDesc { get; set; } = "保存操作权限";

        /// <summary>
        /// 对应用户ID或角色ID
        /// </summary>
        public Guid AppliedID { get; private set; }

        /// <summary>
        /// 对应授权的类型
        /// </summary>
        public PermissionAppliedObjectType AppliedObjectType { get; private set; }

        /// <summary>
        /// 权限分类，方便扩展
        /// </summary>
        public PermissionValidateObjectType ValidateObjectType { get; private set; }
        
        /// <summary>
        /// 功能菜单授权列表
        /// </summary>
        public IList<ObjectPermission> ObjectPermissions { get; private set; }
    }

    public class ObjectPermission
    {
        public ObjectPermission()
        {
            PermissionOpeation = new List<Permission>();
        }

        /// <summary>
        /// 对应功能模块的ID
        /// </summary>
        public Guid AppliedObjectID { get; set; }

        /// <summary>
        /// 权限值转换具体说明集合
        /// </summary>
        public List<Permission> PermissionOpeation { get; set; }
    }
}