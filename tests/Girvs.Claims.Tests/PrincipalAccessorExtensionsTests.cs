namespace Girvs.Claims.Tests;

public class PrincipalAccessorExtensionsTests
{
    [Fact]
    public void ChangeTo_具名字段_作用域内生效并在结束后还原()
    {
        var accessor = new GirvsPrincipalAccessor(new HttpContextAccessor());

        using (accessor.ChangeTo(
                   tenantId: "t1",
                   userName: "系统管理员",
                   source: ExecutionSource.BackgroundJob))
        {
            Assert.True(accessor.Principal.Identity?.IsAuthenticated);
            Assert.Equal("t1", accessor.Principal.GetTenantId());
            Assert.Equal("系统管理员", accessor.Principal.GetUserName());
            Assert.Equal(
                ExecutionSource.BackgroundJob,
                accessor.Principal.GetExecutionSource());
        }

        Assert.Empty(accessor.Principal.Identities);
    }

    [Fact]
    public void ChangeTo_Claim字典_作用域内生效并在结束后还原()
    {
        var accessor = new GirvsPrincipalAccessor(new HttpContextAccessor());
        var claims = new Dictionary<string, string>
        {
            [GirvsClaimTypes.TenantId] = "t1",
            [GirvsClaimTypes.IdentityType] = IdentityType.ManagerUser.ToString(),
        };

        using (accessor.ChangeTo(claims, ExecutionSource.Http))
        {
            Assert.Equal("t1", accessor.Principal.GetTenantId());
            Assert.Equal(IdentityType.ManagerUser, accessor.Principal.GetIdentityType());
        }

        Assert.Empty(accessor.Principal.Identities);
    }

    [Fact]
    public void ChangeTo_嵌套切换_按相反顺序还原()
    {
        var accessor = new GirvsPrincipalAccessor(new HttpContextAccessor());

        using (accessor.ChangeTo(tenantId: "outer"))
        {
            using (accessor.ChangeTo(tenantId: "inner"))
            {
                Assert.Equal("inner", accessor.Principal.GetTenantId());
            }

            Assert.Equal("outer", accessor.Principal.GetTenantId());
        }

        Assert.Empty(accessor.Principal.Identities);
    }
}
