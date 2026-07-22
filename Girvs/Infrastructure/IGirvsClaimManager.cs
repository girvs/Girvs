namespace Girvs.Infrastructure;

public interface IGirvsClaimManager : IManager
{
    string GetTenantId();

    string GetUserId();

    string GetUserName();

    string GetTenantName();

    IdentityType GetIdentityType();

    GirvsIdentityClaim IdentityClaim { get; set; }

    void SetFromHttpRequestToken();

    void SetFromDictionary(Dictionary<string, string> dictionary);

    ClaimsIdentity BuildClaimsIdentity(GirvsIdentityClaim girvsIdentityClaim);
}

public class GirvsIdentityClaim
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string TenantId { get; set; }
    public string TenantName { get; set; }
    public SystemModule SystemModule { get; set; }
    public IdentityType IdentityType { get; set; }
    public Dictionary<string, string> OtherClaims { get; set; } = new Dictionary<string, string>();

    public string this[string key]
    {
        get => OtherClaims[key];
        set => OtherClaims[key] = value;
    }

    public T GetUserId<T>()
    {
        return (T) GirvsConvert.ToSpecifiedType(typeof(T).FullName, UserId);
    }

    public T GetTenantId<T>()
    {
        return (T) GirvsConvert.ToSpecifiedType(typeof(T).FullName, TenantId);
    }
}
