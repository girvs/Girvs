using Girvs.BusinessBasis.Entities;

namespace Girvs.Claims.Tests;

public class IdentityConsumerMigrationTests
{
    [Fact]
    public void BaseEntity_显式Principal_填充创建人与租户字段()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddSingleton<IGirvsPrincipalAccessor, GirvsPrincipalAccessor>();
        var provider = services.BuildServiceProvider();
        var engine = new TestEngine();
        engine.UseProvider(provider);
        EngineContext.Replace(engine);
        var accessor = provider.GetRequiredService<IGirvsPrincipalAccessor>();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(GirvsClaimTypes.UserId, userId.ToString()),
            new Claim(GirvsClaimTypes.UserName, "张三"),
            new Claim(GirvsClaimTypes.TenantId, tenantId.ToString()),
            new Claim(GirvsClaimTypes.TenantName, "租户一")
        ], "Test"));

        using var _ = accessor.Change(principal);
        var entity = new AuditEntity();

        Assert.Equal(userId, entity.CreatorId);
        Assert.Equal("张三", entity.CreatorName);
        Assert.Equal(tenantId, entity.TenantId);
        Assert.Equal("租户一", entity.TenantName);
    }

    private sealed class AuditEntity : BaseEntity<Guid>,
        IIncludeCreatorId<Guid>, IIncludeCreatorName,
        IIncludeMultiTenant<Guid>, IIncludeMultiTenantName
    {
        public Guid CreatorId { get; set; }
        public string CreatorName { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; }
    }

    private sealed class TestEngine : GirvsEngine
    {
        public void UseProvider(IServiceProvider provider) => ServiceProvider = provider;
    }
}
