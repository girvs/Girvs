namespace Girvs.AuthorizePermission;

public static class GirvsAuthorizePermissionCacheKeyManager
{
    public static string CurrentUserAuthorizeCacheKeyPrefix =
        $"Girvs.AuthorizePermission:{AppDomain.CurrentDomain.FriendlyName}";
}