namespace Girvs.Infrastructure;

public static class GirvsClaimTypes
{
    public const string UserId = "zf_sib";
    public const string UserName = "zf_sname";
    public const string TenantId = "zf_tid";
    public const string TenantName = "zf_tname";
    public const string IdentityType = "zf_itype";
    public const string SystemModule = "zf_csm";
    public const string UserType = "zf_utype";
    public const string ExecutionSource = "zf_exec_source";
    public const string ClientId = "client_id";
}

/// <summary>
/// 登录身份类型。
/// </summary>
public enum IdentityType
{
    ManagerUser,
    RegisterUser,
    EventMessageUser
}

/// <summary>
/// 当前代码的执行入口。
/// </summary>
public enum ExecutionSource
{
    Http,
    EventBus,
    BackgroundJob
}

/// <summary>
/// 系统功能模块定义。
/// </summary>
[Flags]
public enum SystemModule : long
{
    BaseModule = 1,
    RegisterModule = 2,
    ArrangeModule = 4,
    IdentityModule = 8,
    ScoreQueryModule = 16,
    SystemModule = 32,
    ExtendModule2 = 64,
    ExtendModule3 = 128,
    ExtendModule4 = 256,
    ExtendModule5 = 512,
    ExtendModule6 = 1024,
    ExtendModule7 = 2048,
    ExtendModule8 = 4096,
    ExtendModule9 = 8192,
    ExtendModule10 = 16384,
    All = BaseModule | RegisterModule | ArrangeModule | IdentityModule | ScoreQueryModule | SystemModule |
          ExtendModule2 | ExtendModule3 | ExtendModule4 | ExtendModule5 | ExtendModule6 |
          ExtendModule7 | ExtendModule8 | ExtendModule9 | ExtendModule10
}
