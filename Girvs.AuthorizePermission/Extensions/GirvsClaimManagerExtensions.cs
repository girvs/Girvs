namespace Girvs.AuthorizePermission.Extensions;

public static class GirvsClaimManagerExtensions
{
    public static string GirvsIdentityUserTypeClaimTypes = "zf_utype";
        
    public static UserType GetUserType(this IGirvsClaimManager claimManager)
    {
        var userTypeStr = claimManager.IdentityClaim[GirvsIdentityUserTypeClaimTypes];
        if (string.IsNullOrEmpty(userTypeStr))
        {
            return UserType.All;
        }

        return (UserType) Enum.Parse(typeof(UserType), userTypeStr);
    }


    public static string GetOtherClaim(this IGirvsClaimManager claimManager, string key)
    {
        return claimManager.IdentityClaim.OtherClaims.TryGetValue(key, out var claim) ? claim : string.Empty;
    }

    public static string GetClientId(this IGirvsClaimManager claimManager)
    {
        return GetOtherClaim(claimManager, "client_id");
    }
}