namespace Girvs.Infrastructure;

/// <summary>
/// 身份访问器扩展。把身份构造与切换合成一步，供没有 HTTP token 的执行入口使用。
/// </summary>
public static class PrincipalAccessorExtensions
{
    /// <summary>
    /// 按具名字段构造身份并在当前异步执行流内切换。
    /// 返回的作用域释放后还原为切换前的身份，业务逻辑须写在作用域内。
    /// </summary>
    public static IDisposable ChangeTo(
        this IGirvsPrincipalAccessor accessor,
        string userId = null,
        string tenantId = null,
        string userName = null,
        string tenantName = null,
        IdentityType identityType = IdentityType.ManagerUser,
        ExecutionSource source = ExecutionSource.Http,
        IDictionary<string, string> additionalClaims = null
    )
    {
        ArgumentNullException.ThrowIfNull(accessor);

        return accessor.Change(
            GirvsPrincipalFactory.Create(
                userId,
                tenantId,
                userName,
                tenantName,
                identityType,
                source,
                additionalClaims
            )
        );
    }

    /// <summary>
    /// 按 claim 字典构造身份并在当前异步执行流内切换。
    /// 返回的作用域释放后还原为切换前的身份，业务逻辑须写在作用域内。
    /// </summary>
    public static IDisposable ChangeTo(
        this IGirvsPrincipalAccessor accessor,
        IDictionary<string, string> claims,
        ExecutionSource source = ExecutionSource.Http
    )
    {
        ArgumentNullException.ThrowIfNull(accessor);

        return accessor.Change(GirvsPrincipalFactory.FromClaims(claims, source));
    }
}
