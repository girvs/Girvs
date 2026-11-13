namespace Girvs.AuthorizePermission.Middleware;

/// <summary>
/// 使用请求头补充注册用户的当前租户身份。
/// </summary>
public sealed class GirvsTenantClaimsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Request.Headers[nameof(GirvsClaimTypes.TenantId)].ToString();
        if (string.IsNullOrEmpty(tenantId))
        {
            await next(context);
            return;
        }

        var tenantName = HttpUtility.UrlDecode(
            context.Request.Headers[nameof(GirvsClaimTypes.TenantName)].ToString());
        var authenticatedIdentity = context.User.Identities.FirstOrDefault(x => x.IsAuthenticated);

        if (authenticatedIdentity != null)
        {
            if (context.User.GetIdentityType() == IdentityType.RegisterUser)
            {
                ReplaceClaim(authenticatedIdentity, GirvsClaimTypes.TenantId, tenantId);
                ReplaceClaim(authenticatedIdentity, GirvsClaimTypes.TenantName, tenantName);
            }
        }
        else
        {
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(GirvsClaimTypes.TenantId, tenantId));
            if (!string.IsNullOrEmpty(tenantName))
            {
                identity.AddClaim(new Claim(GirvsClaimTypes.TenantName, tenantName));
            }
            identity.AddClaim(new Claim(
                GirvsClaimTypes.IdentityType,
                IdentityType.RegisterUser.ToString()));
            context.User.AddIdentity(identity);
        }

        await next(context);
    }

    private static void ReplaceClaim(ClaimsIdentity identity, string claimType, string value)
    {
        foreach (var claim in identity.FindAll(claimType).ToList())
        {
            identity.RemoveClaim(claim);
        }

        if (!string.IsNullOrEmpty(value))
        {
            identity.AddClaim(new Claim(claimType, value));
        }
    }
}
