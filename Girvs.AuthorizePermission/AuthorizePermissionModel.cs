namespace Girvs.AuthorizePermission;

/// <summary>
/// 功能模块授权模型
/// </summary>
/// <param name="ServiceName">服务名称</param>
/// <param name="ServiceId">服务功能模块Id</param>
/// <param name="Tag">所属标签</param>
/// <param name="Order">排序</param>
/// <param name="SystemModule">所属的子系统模块</param>
/// <param name="OtherParams">其它相关参数</param>
/// <param name="OperationPermissionModels"></param>
/// <param name="Permissions"></param>
public record AuthorizePermissionModel(string ServiceName, Guid ServiceId, string Tag, int Order,
    SystemModule SystemModule, string[] OtherParams, List<OperationPermissionModel> OperationPermissionModels,
    Dictionary<string, string> Permissions) : IDto;

/// <summary>
/// 操作权限模型
/// </summary>
/// <param name="OperationName">操作名称</param>
/// <param name="Permission">权限值</param>
/// <param name="UserType">用户类型</param>
/// <param name="SystemModule">所属的子系统模块</param>
/// <param name="OtherParams">其它相关参数</param>
public record OperationPermissionModel(string OperationName, Permission Permission, UserType UserType,
    SystemModule SystemModule, string[] OtherParams) : IDto;