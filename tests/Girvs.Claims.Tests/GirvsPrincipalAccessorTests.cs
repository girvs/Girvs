namespace Girvs.Claims.Tests;

public class GirvsPrincipalAccessorTests
{
    [Fact]
    public void Principal_没有显式身份时_返回HttpContextUser()
    {
        var httpPrincipal = CreatePrincipal("http-user");
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = httpPrincipal }
        };
        var accessor = new GirvsPrincipalAccessor(httpContextAccessor);

        Assert.Same(httpPrincipal, accessor.Principal);
    }

    [Fact]
    public void Principal_没有任何身份时_返回空Principal()
    {
        var accessor = new GirvsPrincipalAccessor(new HttpContextAccessor());

        Assert.NotNull(accessor.Principal);
        Assert.Empty(accessor.Principal.Identities);
    }

    [Fact]
    public void Change_嵌套切换身份_按相反顺序恢复()
    {
        var httpPrincipal = CreatePrincipal("http-user");
        var accessor = CreateAccessor(httpPrincipal);
        var first = CreatePrincipal("first-user");
        var second = CreatePrincipal("second-user");

        using (accessor.Change(first))
        {
            Assert.Same(first, accessor.Principal);
            using (accessor.Change(second))
            {
                Assert.Same(second, accessor.Principal);
            }
            Assert.Same(first, accessor.Principal);
        }

        Assert.Same(httpPrincipal, accessor.Principal);
    }

    [Fact]
    public async Task Change_并发异步流_身份互不串扰()
    {
        var httpPrincipal = CreatePrincipal("http-user");
        var accessor = CreateAccessor(httpPrincipal);
        var barrier = new Barrier(2);

        async Task<string> RunAsync(string userId)
        {
            using (accessor.Change(CreatePrincipal(userId)))
            {
                await Task.Yield();
                barrier.SignalAndWait();
                await Task.Yield();
                return accessor.Principal.FindFirstValue("user_id");
            }
        }

        var results = await Task.WhenAll(RunAsync("user-a"), RunAsync("user-b"));

        Assert.Equal(["user-a", "user-b"], results);
        Assert.Same(httpPrincipal, accessor.Principal);
    }

    private static GirvsPrincipalAccessor CreateAccessor(ClaimsPrincipal principal)
    {
        return new GirvsPrincipalAccessor(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        });
    }

    private static ClaimsPrincipal CreatePrincipal(string userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim("user_id", userId)], "Test"));
    }
}
