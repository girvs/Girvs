namespace Girvs.AuthorizePermission.Extensions;

public static class ClaimsPrincipalAuthorizeExtensions
{
    public static UserType GetUserType(this ClaimsPrincipal principal)
    {
        var value = principal.GetClaimValue(GirvsClaimTypes.UserType);
        return Enum.TryParse<UserType>(value, out var userType) ? userType : UserType.All;
    }

    public static string GetClientId(this ClaimsPrincipal principal) =>
        principal.GetClaimValue(GirvsClaimTypes.ClientId);
}
