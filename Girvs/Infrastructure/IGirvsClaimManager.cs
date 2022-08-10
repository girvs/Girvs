namespace Girvs.Infrastructure;

public interface IGirvsClaimManager : IManager
{
    string GetTenantId();

    string GetUserId();

    string GetUserName();

    string GetTenantName();

    IdentityType GetIdentityType();

    GirvsIdentityClaim IdentityClaim { get; set; }

    void SetFromHttpRequestToken();

    void SetFromDictionary(Dictionary<string, string> dictionary);

    ClaimsIdentity BuildClaimsIdentity(GirvsIdentityClaim girvsIdentityClaim);
}

public static class GirvsIdentityClaimTypes
{
    public static readonly string UserId = ClaimTypes.Sid;
    public static readonly string UserName = ClaimTypes.Name;
    public static readonly string TenantId = ClaimTypes.GroupSid;
    public static readonly string TenantName = ClaimTypes.GivenName;
    public static readonly string IdentityType = ClaimTypes.Locality;
    public static readonly string ClaimSystemModule = ClaimTypes.System;
}

public class GirvsIdentityClaim
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string TenantId { get; set; }
    public string TenantName { get; set; }
    public SystemModule SystemModule { get; set; }
    public IdentityType IdentityType { get; set; }
    public Dictionary<string, string> OtherClaims { get; set; } = new Dictionary<string, string>();

    public string this[string key]
    {
        get => OtherClaims[key];
        set => OtherClaims[key] = value;
    }

    public T GetUserId<T>()
    {
        return (T) GirvsConvert.ToSpecifiedType(typeof(T).FullName, UserId);
    }

    public T GetTenantId<T>()
    {
        return (T) GirvsConvert.ToSpecifiedType(typeof(T).FullName, TenantId);
    }
}

/// <summary>
/// 登陆身份类型
/// </summary>
public enum IdentityType
{
    /// <summary>
    /// 后台管理用户
    /// </summary>
    ManagerUser,

    /// <summary>
    /// 前台注册用户
    /// </summary>
    RegisterUser,

    /// <summary>
    /// 事件触发用户
    /// </summary>
    EventMessageUser
}

/// <summary>
/// 系统功能模块定义（也可以称为各子系统的定义）
/// </summary>
[Flags]
public enum SystemModule : long
{
    /// <summary>
    /// 基本功能
    /// </summary>
    BaseModule = 1,

    /// <summary>
    /// 网报
    /// </summary>
    RegisterModule = 2,

    /// <summary>
    /// 编排
    /// </summary>
    ArrangeModule = 4,

    /// <summary>
    /// 准考证
    /// </summary>
    IdentityModule = 8,

    /// <summary>
    /// 成绩查询
    /// </summary>
    ScoreQueryModule = 16,

    /// <summary>
    /// 扩展1
    /// </summary>
    ExtendModule1 = 32,

    /// <summary>
    /// 扩展2
    /// </summary>
    ExtendModule2 = 64,

    /// <summary>
    /// 扩展3
    /// </summary>
    ExtendModule3 = 128,

    /// <summary>
    /// 扩展4
    /// </summary>
    ExtendModule4 = 256,

    /// <summary>
    /// 扩展5
    /// </summary>
    ExtendModule5 = 512,

    /// <summary>
    /// 扩展6
    /// </summary>
    ExtendModule6 = 1024,

    /// <summary>
    /// 扩展7
    /// </summary>
    ExtendModule7 = 2048,

    /// <summary>
    /// 扩展8
    /// </summary>
    ExtendModule8 = 4096,

    /// <summary>
    /// 扩展9
    /// </summary>
    ExtendModule9 = 8192,

    /// <summary>
    /// 扩展10
    /// </summary>
    ExtendModule10 = 16384,

    /// <summary>
    /// 所有
    /// </summary>
    All = BaseModule | RegisterModule | ArrangeModule | IdentityModule | ScoreQueryModule | ExtendModule1 |
          ExtendModule2 | ExtendModule3 | ExtendModule4 | ExtendModule5 | ExtendModule6 |
          ExtendModule7 | ExtendModule8 | ExtendModule9 | ExtendModule10
}