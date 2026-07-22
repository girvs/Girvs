namespace Girvs.Claims.Tests;

public class JwtBearerAuthenticationExtensionTests
{
    public JwtBearerAuthenticationExtensionTests()
    {
        var settings = new AppSettings();
        settings.PreLoadModelConfig();
        settings[nameof(AuthorizeConfig)] = new AuthorizeConfig
        {
            JwtConfig = new JwtConfig
            {
                Secret = "ClaimsPrincipal_Tests_Secret_1234567890",
                ExpiresHours = 1
            }
        };
        Singleton<AppSettings>.Instance = settings;
    }

    [Fact]
    public void GenerateToken_参数式重载_生成全部GirvsClaim()
    {
        var token = JwtBearerAuthenticationExtension.GenerateToken(
            "user-1",
            "张三",
            "tenant-1",
            "租户一",
            UserType.TenantAdminUser,
            IdentityType.RegisterUser,
            SystemModule.ArrangeModule);

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToList();

        Assert.Contains(claims, x => x.Type == GirvsClaimTypes.UserId && x.Value == "user-1");
        Assert.Contains(claims, x => x.Type == GirvsClaimTypes.UserName && x.Value == "张三");
        Assert.Contains(claims, x => x.Type == GirvsClaimTypes.TenantId && x.Value == "tenant-1");
        Assert.Contains(claims, x => x.Type == GirvsClaimTypes.TenantName && x.Value == "租户一");
        Assert.Contains(claims, x => x.Type == GirvsClaimTypes.UserType && x.Value == UserType.TenantAdminUser.ToString());
        Assert.Contains(claims, x => x.Type == GirvsClaimTypes.IdentityType && x.Value == IdentityType.RegisterUser.ToString());
        Assert.Contains(claims, x => x.Type == GirvsClaimTypes.SystemModule && x.Value == SystemModule.ArrangeModule.ToString());
    }

    [Fact]
    public void GenerateToken_传入同类型多值Claim_全部保留()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("role", "reader"),
            new Claim("role", "writer")
        ]);

        var token = JwtBearerAuthenticationExtension.GenerateToken(identity);
        var roles = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims
            .Where(x => x.Type == "role")
            .Select(x => x.Value);

        Assert.Equal(["reader", "writer"], roles);
    }
}
