using System;
using System.ComponentModel.DataAnnotations.Schema;
using Girvs.Domain.Enumerations;

namespace Power.BasicManagement.Domain.Models
{
    /// <summary>
    /// BasalPermission 对象影射到表 'Permissions'.
    /// </summary>
    public class BasalPermission : PermissionBase
    {
        #region Public Properties

        /// <summary>
        /// 对应用户ID或角色ID
        /// </summary>
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
        /// 权限值
        /// </summary>
        public int ValidateObjectID { get; set; }

        /// <summary>
        /// 权限分类，方便扩展
        /// </summary>
        public PermissionValidateObjectType ValidateObjectType { get; set; }

        #endregion

        #region Public Properties

        [NotMapped]
        public virtual bool View => GetBit(Permission.View);

        [NotMapped]
        public virtual bool Read => GetBit(Permission.Read);

        [NotMapped]
        public virtual bool Read_Extend1 => GetBit(Permission.Read_Extend1);

        [NotMapped]
        public virtual bool Post => GetBit(Permission.Post);

        [NotMapped]
        public virtual bool Relation => GetBit(Permission.Relation);

        [NotMapped]
        public virtual bool Post_Extend1 => GetBit(Permission.Post_Extend1);

        [NotMapped]
        public virtual bool Post_Extend2 => GetBit(Permission.Post_Extend2);

        [NotMapped]
        public virtual bool Reply => GetBit(Permission.Reply);

        [NotMapped]
        public virtual bool Edit => GetBit(Permission.Edit);

        [NotMapped]
        public virtual bool Edit_Extend1 => GetBit(Permission.Edit_Extend1);

        [NotMapped]
        public virtual bool Edit_Extend2 => GetBit(Permission.Edit_Extend2);

        [NotMapped]
        public virtual bool Delete => GetBit(Permission.Delete);

        [NotMapped]
        public virtual bool Delete_Extend1 => GetBit(Permission.Delete_Extend1);

        [NotMapped]
        public virtual bool Delete_Extend2 => GetBit(Permission.Delete_Extend2);

        [NotMapped]
        public virtual bool Audit => GetBit(Permission.Audit);

        [NotMapped]
        public virtual bool Reject => GetBit(Permission.Reject);

        [NotMapped]
        public new virtual bool Merge => GetBit(Permission.Merge);

        [NotMapped]
        public virtual bool Cancel => GetBit(Permission.Cancel);

        [NotMapped]
        public virtual bool Publish => GetBit(Permission.Publish);

        [NotMapped]
        public virtual bool Move => GetBit(Permission.Move);

        [NotMapped]
        public virtual bool Copy => GetBit(Permission.Copy);

        [NotMapped]
        public virtual bool Vote => GetBit(Permission.Vote);

        [NotMapped]
        public virtual bool LocalAttachment => GetBit(Permission.LocalAttachment);

        [NotMapped]
        public virtual bool Catalog_Read => GetBit(Permission.Catalog_Read);

        [NotMapped]
        public virtual bool Administer => GetBit(Permission.Administer);

        [NotMapped]
        public virtual bool SystemAdmin => GetBit(Permission.SystemAdmin);

        #endregion
    }

}
