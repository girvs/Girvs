namespace Girvs.AuthorizePermission.Enumerations;

[Flags]
public enum UserType
{
    /// <summary>
    /// 超级管理员
    /// </summary>
    [Description("管理员用户")] AdminUser = 1,

    /// <summary>
    /// 普通用户
    /// </summary>
    [Description("普通用户")] GeneralUser = 2,

    /// <summary>
    /// 特殊用户
    /// </summary>
    [Description("特殊用户")] SpecialUser = 4,

    /// <summary>
    /// 初级用户
    /// </summary>
    [Description("初级用户")] LowUser = 8,

    /// <summary>
    /// 中级用户
    /// </summary>
    [Description("中级用户")] IntermediateUser = 16,

    /// <summary>
    /// 高级用户
    /// </summary>
    [Description("高级用户")] HighUser = 32,

    /// <summary>
    /// 租户管理员
    /// </summary>
    [Description("租户管理员")] TenantAdminUser = 64,


    [Description("租户平台级别需要的用户类型")]Tenant = GeneralUser | TenantAdminUser,

    [Description("系统级别所需要的用户类型")]Platform = AdminUser | SpecialUser,

    /// <summary>
    /// 所有用户
    /// </summary>
    [Description("所有用户")] All = AdminUser | GeneralUser | SpecialUser | LowUser | IntermediateUser | HighUser |
                                TenantAdminUser
}