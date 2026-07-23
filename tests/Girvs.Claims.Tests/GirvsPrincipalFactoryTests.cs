namespace Girvs.Claims.Tests;

public class GirvsPrincipalFactoryTests
{
    [Fact]
    public void Create_产出的身份_已通过认证()
    {
        var principal = GirvsPrincipalFactory.Create(userId: "u1");

        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Equal(
            GirvsPrincipalFactory.AuthenticationType,
            principal.Identity?.AuthenticationType);
    }

    [Fact]
    public void Create_具名字段_映射到对应的GirvsClaimTypes()
    {
        var principal = GirvsPrincipalFactory.Create(
            userId: "u1",
            tenantId: "t1",
            userName: "系统管理员",
            tenantName: "系统",
            identityType: IdentityType.EventMessageUser,
            source: ExecutionSource.BackgroundJob);

        Assert.Equal("u1", principal.GetUserId());
        Assert.Equal("t1", principal.GetTenantId());
        Assert.Equal("系统管理员", principal.GetUserName());
        Assert.Equal("系统", principal.GetTenantName());
        Assert.Equal(IdentityType.EventMessageUser, principal.GetIdentityType());
        Assert.Equal(ExecutionSource.BackgroundJob, principal.GetExecutionSource());
    }

    [Fact]
    public void Create_未传的字段_不产生Claim()
    {
        var principal = GirvsPrincipalFactory.Create(tenantId: "t1");

        Assert.Null(principal.FindFirst(GirvsClaimTypes.UserId));
        Assert.Equal(string.Empty, principal.GetUserId());
    }

    [Fact]
    public void Create_未指定身份类型与执行入口时_取默认值()
    {
        var principal = GirvsPrincipalFactory.Create(tenantId: "t1");

        Assert.Equal(IdentityType.ManagerUser, principal.GetIdentityType());
        Assert.Equal(ExecutionSource.Http, principal.GetExecutionSource());
    }

    [Fact]
    public void Create_附加Claim与具名字段冲突时_具名字段优先()
    {
        var principal = GirvsPrincipalFactory.Create(
            tenantId: "t1",
            additionalClaims: new Dictionary<string, string>
            {
                [GirvsClaimTypes.TenantId] = "t-additional",
                [GirvsClaimTypes.ClientId] = "client-1",
            });

        Assert.Equal("t1", principal.GetTenantId());
        Assert.Equal("client-1", principal.GetClaimValue(GirvsClaimTypes.ClientId));
    }

    [Fact]
    public void Create_具名字段为空时_保留附加Claim中的同名项()
    {
        var principal = GirvsPrincipalFactory.Create(
            additionalClaims: new Dictionary<string, string>
            {
                [GirvsClaimTypes.TenantId] = "t-additional",
            });

        Assert.Equal("t-additional", principal.GetTenantId());
    }
}
