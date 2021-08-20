using System;
using System.ComponentModel;

namespace Girvs.AuthorizePermission.Enumerations
{
    /// <summary>
    /// 权限标志枚举
    /// </summary>
    [Flags()]
    public enum Permission : long
    {
        /// <summary>
        /// 未定义
        /// </summary>
        [Description("未定义")]
        Undefined = 0,
        /// <summary>
        /// 浏览
        /// </summary>
        [Description("浏览")]
        View = 0x0000000000000001,
        /// <summary>
        /// 读取
        /// </summary>
        [Description("读取")]
        Read = 0x0000000000000002,
        /// <summary>
        /// 读取扩展1，该预留枚举项用于某些存在几种类似于读取操作的应用程序中
        /// </summary>
        [Description("读取扩展1")]
        Read_Extend1 = 0x0000000000000004,
        /// <summary>
        /// 新增/发表
        /// </summary>
        [Description("新增/发表")]
        Post = 0x0000000000000008,
        /// <summary>
        /// 发表扩展1，该预留枚举项用于某些存在几种类似于发表操作的应用程序中
        /// </summary>
        [Description("发表扩展1")]
        Post_Extend1 = 0x0000000000000010,
        /// <summary>
        /// 发表扩展2，该预留枚举项用于某些存在几种类似于发表操作的应用程序中
        /// </summary>
        [Description("发表扩展2")]
        Post_Extend2 = 0x0000000000000020,
        /// <summary>
        /// 回复
        /// </summary>
        [Description("回复")]
        Reply = 0x0000000000000040,
        /// <summary>
        /// 编辑
        /// </summary>
        [Description("编辑")]
        Edit = 0x0000000000000080,
        /// <summary>
        /// 编辑扩展1，该预留枚举项用于某些存在几种类似于编辑操作的应用程序中
        /// </summary>
        [Description("编辑扩展1")]
        Edit_Extend1 = 0x0000000000000100,
        /// <summary>
        /// 编辑扩展2，该预留枚举项用于某些存在几种类似于编辑操作的应用程序中
        /// </summary>
        [Description("编辑扩展2")]
        Edit_Extend2 = 0x0000000000000200,
        /// <summary>
        /// 删除
        /// </summary>
        [Description("删除")]
        Delete = 0x0000000000000400,
        /// <summary>
        /// 删除扩展1，该预留枚举项用于某些存在几种类似于删除操作的应用程序中
        /// </summary>
        [Description("删除扩展1")]
        Delete_Extend1 = 0x0000000000000800,
        /// <summary>
        /// 删除扩展2，该预留枚举项用于某些存在几种类似于删除操作的应用程序中
        /// </summary>
        [Description("删除扩展2")]
        Delete_Extend2 = 0x0000000000001000,
        /// <summary>
        /// 审核
        /// </summary>
        [Description("审核")]
        Audit = 0x0000000000002000,
        /// <summary>
        /// 拒绝/驳回
        /// </summary>
        [Description("拒绝/驳回")]
        Reject = 0x0000000000004000,
        /// <summary>
        /// 合并
        /// </summary>
        [Description("合并")]
        Merge = 0x0000000000008000,
        /// <summary>
        /// 撤消、取消
        /// </summary>
        [Description("撤消、取消")]
        Cancel = 0x0000000000010000,
        /// <summary>
        /// 发布
        /// </summary>
        [Description("发布")]
        Publish = 0x0000000000020000,
        /// <summary>
        /// 移动
        /// </summary>
        [Description("移动")]
        Move = 0x0000000000040000,
        /// <summary>
        /// 复制
        /// </summary>
        [Description("复制")]
        Copy = 0x0000000000080000,
        /// <summary>
        /// 投票
        /// </summary>
        [Description("投票")]
        Vote = 0x0000000000100000,
        /// <summary>
        /// 添加本地附件
        /// </summary>
        [Description("添加本地附件")]
        LocalAttachment = 0x0000000000200000,
        /// <summary>
        /// 读取栏目
        /// </summary>
        [Description("读取栏目")]
        Catalog_Read = 0x0000000000400000,
        /// <summary>
        /// 关联
        /// </summary>
        [Description("关联")]
        Relation = 0x0000000000800000,
        /// <summary>
        /// 管理
        /// </summary>
        [Description("管理")]
        Administer = 0x0100000000000000,
        /// <summary>
        /// 系统管理
        /// </summary>
        [Description("系统管理")]
        SystemAdmin = 0x4000000000000000,
    }

    public enum AccessControlEntry
    {
        NotSet = 0x00,
        Allow = 0x01,
        Deny = 0x02
    }


    /// <summary>
    /// 要验证的对象类型，表示是验证栏目权限还是菜单权限
    /// </summary>
    public enum PermissionValidateObjectType
    {
        /// <summary>
        /// 功能菜单
        /// </summary>
        FunctionMenu = 0
    }

    /// <summary>
    /// 权限所应用的对象类型，表示权限是应用到角色上还是应用到用户上
    /// </summary>
    public enum PermissionAppliedObjectType
    {
        ///
        /// 角色
        /// </summary>
        Role,
        User
    }
}
