namespace Girvs.AuthorizePermission.Services;

public interface IGirvsAuthorizePermissionService : IAppWebApiService, IManager
{
    Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList();

    Task<List<AuthorizeDataRuleModel>> GetAuthorizeDataRuleList();
}