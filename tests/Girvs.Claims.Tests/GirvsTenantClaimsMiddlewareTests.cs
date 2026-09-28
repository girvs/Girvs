namespace Girvs.Claims.Tests;

public class GirvsTenantClaimsMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_已认证注册用户_请求头租户替换原租户Claim()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(GirvsClaimTypes.IdentityType, IdentityType.RegisterUser.ToString()),
            new Claim(GirvsClaimTypes.TenantId, "old-tenant"),
            new Claim(GirvsClaimTypes.TenantName, "旧租户"),
            new Claim("role", "reader")
        ], "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        context.Request.Headers[nameof(GirvsClaimTypes.TenantId)] = "new-tenant";
        context.Request.Headers[nameof(GirvsClaimTypes.TenantName)] = "%E7%A7%9F%E6%88%B7%E4%B8%80";
        var middleware = new GirvsTenantClaimsMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("new-tenant", context.User.GetTenantId());
        Assert.Equal("租户一", context.User.GetTenantName());
        Assert.Equal("reader", context.User.FindFirst("role")?.Value);
        Assert.Single(context.User.FindAll(GirvsClaimTypes.TenantId));
    }

    [Fact]
    public async Task InvokeAsync_匿名请求携带租户头_补充租户但不标记认证()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[nameof(GirvsClaimTypes.TenantId)] = "tenant-1";
        context.Request.Headers[nameof(GirvsClaimTypes.TenantName)] = "tenant-name";
        var middleware = new GirvsTenantClaimsMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("tenant-1", context.User.GetTenantId());
        Assert.Equal(IdentityType.RegisterUser, context.User.GetIdentityType());
        Assert.False(context.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task InvokeAsync_没有租户头_不修改Principal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], "Test"));
        var context = new DefaultHttpContext { User = principal };
        var middleware = new GirvsTenantClaimsMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Same(principal, context.User);
    }
}
