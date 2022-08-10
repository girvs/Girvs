namespace Girvs.AuthorizePermission.AuthorizeCompare
{
    public interface IServiceMethodPermissionCompare: IManager
    {
        bool PermissionCompare(Guid functionId, Permission permission);
    }
}