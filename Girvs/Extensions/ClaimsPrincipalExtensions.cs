namespace Girvs.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal) =>
        principal.GetClaimValue(GirvsClaimTypes.UserId);

    public static T GetUserId<T>(this ClaimsPrincipal principal) =>
        ConvertClaimValue<T>(principal.GetUserId());

    public static string GetUserName(this ClaimsPrincipal principal) =>
        principal.GetClaimValue(GirvsClaimTypes.UserName);

    public static string GetTenantId(this ClaimsPrincipal principal) =>
        principal.GetClaimValue(GirvsClaimTypes.TenantId);

    public static T GetTenantId<T>(this ClaimsPrincipal principal) =>
        ConvertClaimValue<T>(principal.GetTenantId());

    public static string GetTenantName(this ClaimsPrincipal principal) =>
        principal.GetClaimValue(GirvsClaimTypes.TenantName);

    public static IdentityType GetIdentityType(this ClaimsPrincipal principal) =>
        ParseEnum(principal.GetClaimValue(GirvsClaimTypes.IdentityType), IdentityType.ManagerUser);

    public static SystemModule GetSystemModule(this ClaimsPrincipal principal) =>
        ParseEnum(principal.GetClaimValue(GirvsClaimTypes.SystemModule), SystemModule.All);

    public static ExecutionSource GetExecutionSource(this ClaimsPrincipal principal) =>
        ParseEnum(principal.GetClaimValue(GirvsClaimTypes.ExecutionSource), ExecutionSource.Http);

    public static string GetClaimValue(this ClaimsPrincipal principal, string claimType)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrEmpty(claimType);
        return principal.FindFirst(claimType)?.Value ?? string.Empty;
    }

    private static T ConvertClaimValue<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        return (T)GirvsConvert.ToSpecifiedType(typeof(T).FullName, value);
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, out var result) ? result : defaultValue;
    }
}
