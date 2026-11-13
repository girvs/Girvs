namespace Girvs.Infrastructure;

/// <summary>
/// 当前执行上下文的身份访问器。
/// </summary>
public interface IGirvsPrincipalAccessor
{
    /// <summary>
    /// 获取当前身份。
    /// </summary>
    ClaimsPrincipal Principal { get; }

    /// <summary>
    /// 在当前异步执行流内临时切换身份。
    /// </summary>
    IDisposable Change(ClaimsPrincipal principal);
}
