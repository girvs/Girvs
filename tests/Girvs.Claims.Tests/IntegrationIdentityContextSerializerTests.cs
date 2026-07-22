namespace Girvs.Claims.Tests;

public class IntegrationIdentityContextSerializerTests
{
    [Fact]
    public void Serialize_混合Claims_只保留白名单()
    {
        var principal = CreatePrincipal(
            new Claim(GirvsClaimTypes.TenantId, "tenant-1"),
            new Claim(GirvsClaimTypes.ClientId, "client-1"),
            new Claim("role", "admin"),
            new Claim("permission", "write"),
            new Claim("Authorization", "secret"));

        var json = IntegrationIdentityContextSerializer.Serialize(principal);
        var identity = IntegrationIdentityContextSerializer.Deserialize(json);

        Assert.Equal(2, identity.Claims.Count);
        Assert.Contains(identity.Claims, x => x.Type == GirvsClaimTypes.TenantId);
        Assert.Contains(identity.Claims, x => x.Type == GirvsClaimTypes.ClientId);
    }

    [Fact]
    public void Serialize_同类型多值Claim_往返后全部保留()
    {
        var principal = CreatePrincipal(
            new Claim(GirvsClaimTypes.ClientId, "client-1"),
            new Claim(GirvsClaimTypes.ClientId, "client-2"));

        var identity = IntegrationIdentityContextSerializer.Deserialize(
            IntegrationIdentityContextSerializer.Serialize(principal));

        Assert.Equal(["client-1", "client-2"], identity.Claims.Select(x => x.Value));
    }

    [Fact]
    public void Serialize_没有白名单Claim_返回Null()
    {
        var principal = CreatePrincipal(new Claim("role", "admin"));

        Assert.Null(IntegrationIdentityContextSerializer.Serialize(principal));
    }

    [Fact]
    public void Serialize_超过8192字节_异常不泄露Claim值()
    {
        var secret = new string('x', IntegrationIdentityContextSerializer.MaxHeaderBytes);
        var principal = CreatePrincipal(new Claim(GirvsClaimTypes.UserName, secret));

        var exception = Assert.Throws<GirvsException>(() =>
            IntegrationIdentityContextSerializer.Serialize(principal));

        Assert.DoesNotContain(secret, exception.Message);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"Version\":2,\"Claims\":[]}")]
    public void Deserialize_非法Json或未知版本_抛GirvsException(string json)
    {
        Assert.Throws<GirvsException>(() => IntegrationIdentityContextSerializer.Deserialize(json));
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test"));
}
