namespace Girvs.Claims.Tests;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void 身份扩展_读取全部GirvsClaim()
    {
        var principal = CreatePrincipal(
            new Claim(GirvsClaimTypes.UserId, "user-1"),
            new Claim(GirvsClaimTypes.UserName, "张三"),
            new Claim(GirvsClaimTypes.TenantId, Guid.Empty.ToString()),
            new Claim(GirvsClaimTypes.TenantName, "租户一"),
            new Claim(GirvsClaimTypes.IdentityType, IdentityType.RegisterUser.ToString()),
            new Claim(GirvsClaimTypes.SystemModule, SystemModule.ArrangeModule.ToString()),
            new Claim(GirvsClaimTypes.ExecutionSource, ExecutionSource.EventBus.ToString()));

        Assert.Equal("user-1", principal.GetUserId());
        Assert.Equal("张三", principal.GetUserName());
        Assert.Equal(Guid.Empty.ToString(), principal.GetTenantId());
        Assert.Equal("租户一", principal.GetTenantName());
        Assert.Equal(IdentityType.RegisterUser, principal.GetIdentityType());
        Assert.Equal(SystemModule.ArrangeModule, principal.GetSystemModule());
        Assert.Equal(ExecutionSource.EventBus, principal.GetExecutionSource());
    }

    [Fact]
    public void FindAll_同类型多值Claim_全部保留()
    {
        var principal = CreatePrincipal(new Claim("role", "reader"), new Claim("role", "writer"));

        Assert.Equal(["reader", "writer"], principal.FindAll("role").Select(x => x.Value));
    }

    [Fact]
    public void GetTenantId_泛型Guid_转换成功()
    {
        var tenantId = Guid.NewGuid();
        var principal = CreatePrincipal(new Claim(GirvsClaimTypes.TenantId, tenantId.ToString()));

        Assert.Equal(tenantId, principal.GetTenantId<Guid>());
    }

    [Fact]
    public void 身份扩展_缺少Claim_返回约定默认值()
    {
        var principal = new ClaimsPrincipal();

        Assert.Equal(string.Empty, principal.GetUserId());
        Assert.Equal(string.Empty, principal.GetTenantId());
        Assert.Equal(Guid.Empty, principal.GetTenantId<Guid>());
        Assert.Equal(IdentityType.ManagerUser, principal.GetIdentityType());
        Assert.Equal(SystemModule.All, principal.GetSystemModule());
        Assert.Equal(ExecutionSource.Http, principal.GetExecutionSource());
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
