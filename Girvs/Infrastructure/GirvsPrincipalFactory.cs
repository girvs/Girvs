namespace Girvs.Infrastructure;

/// <summary>
/// 身份构造工厂。用于定时任务、接口回调、启动期发送事件消息等没有 HTTP token 的执行入口。
/// </summary>
public static class GirvsPrincipalFactory
{
    /// <summary>
    /// 认证方式名称。必须传给 <see cref="ClaimsIdentity"/>，否则 IsAuthenticated 为 false。
    /// </summary>
    public const string AuthenticationType = "Girvs";

    /// <summary>
    /// 按具名字段构造身份。字段为空时不产生对应的 Claim；
    /// <paramref name="additionalClaims"/> 中与具名字段同类型的项会被具名字段覆盖。
    /// </summary>
    public static ClaimsPrincipal Create(
        string userId = null,
        string tenantId = null,
        string userName = null,
        string tenantName = null,
        IdentityType identityType = IdentityType.ManagerUser,
        ExecutionSource source = ExecutionSource.Http,
        IDictionary<string, string> additionalClaims = null)
    {
        var claims = new Dictionary<string, string>();

        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                if (!string.IsNullOrEmpty(claim.Key) && claim.Value != null)
                {
                    claims[claim.Key] = claim.Value;
                }
            }
        }

        SetIfNotEmpty(claims, GirvsClaimTypes.UserId, userId);
        SetIfNotEmpty(claims, GirvsClaimTypes.TenantId, tenantId);
        SetIfNotEmpty(claims, GirvsClaimTypes.UserName, userName);
        SetIfNotEmpty(claims, GirvsClaimTypes.TenantName, tenantName);
        claims[GirvsClaimTypes.IdentityType] = identityType.ToString();
        claims[GirvsClaimTypes.ExecutionSource] = source.ToString();

        return Build(claims);
    }

    private static void SetIfNotEmpty(
        IDictionary<string, string> claims,
        string claimType,
        string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            claims[claimType] = value;
        }
    }

    private static ClaimsPrincipal Build(IDictionary<string, string> claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Key, claim.Value)),
            AuthenticationType);

        return new ClaimsPrincipal(identity);
    }
}
