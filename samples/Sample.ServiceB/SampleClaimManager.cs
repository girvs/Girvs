using System.Security.Claims;
using Girvs.Infrastructure;

namespace Sample.ServiceB;

/// <summary>
/// 最小 IGirvsClaimManager 实现。样例未引入认证模块（Girvs.AuthorizePermission），
/// 而 CapEventBus.PublishAsync 会访问 EngineContext.Current.ClaimManager.IdentityClaim
/// （框架该处的 ?. 只保护了 IdentityClaim、未保护 ClaimManager 本身），ClaimManager 为 null 会 NRE。
/// 真实服务由 AuthorizePermission 提供 ClaimManager；此处注册空实现以隔离验证 EventBus。
/// </summary>
public class SampleClaimManager : IGirvsClaimManager
{
    public GirvsIdentityClaim IdentityClaim { get; set; } = new();

    public string GetTenantId() => string.Empty;
    public string GetUserId() => string.Empty;
    public string GetUserName() => string.Empty;
    public string GetTenantName() => string.Empty;
    public IdentityType GetIdentityType() => IdentityType.EventMessageUser;
    public void SetFromHttpRequestToken() { }
    public void SetFromDictionary(Dictionary<string, string> dictionary) { }
    public ClaimsIdentity BuildClaimsIdentity(GirvsIdentityClaim girvsIdentityClaim) => new();
}
