namespace Girvs.AuthorizePermission;

public record AuthorizeModel(List<AuthorizeDataRuleModel> AuthorizeDataRules,
    List<AuthorizePermissionModel> AuthorizePermissions);