namespace Girvs.AuthorizePermission.Services;

[DynamicWebApi]
[AllowAnonymous]
public class GirvsAuthorizePermissionService : IGirvsAuthorizePermissionService
{
    public Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList()
    {
        var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
        var services = typeFinder.FindOfType<IAppWebApiService>()
            .Where(x => x.IsDefined(typeof(ServicePermissionDescriptorAttribute), false));

        var list = services.Select(service =>
        {
            var spd =
                service.GetCustomAttribute(typeof(ServicePermissionDescriptorAttribute)) as
                    ServicePermissionDescriptorAttribute;

            var methodInfos = service.GetMethods().Where(x =>
                x.IsPublic && x.IsDefined(typeof(ServiceMethodPermissionDescriptorAttribute), false));


            var operationPermissionModels = new List<OperationPermissionModel>();
            foreach (var methodInfo in methodInfos)
            {
                var smpd =
                    methodInfo.GetCustomAttribute(typeof(ServiceMethodPermissionDescriptorAttribute)) as
                        ServiceMethodPermissionDescriptorAttribute;

                if (operationPermissionModels.Any(x => x.OperationName == smpd.MethodName))
                    continue;

                operationPermissionModels.Add(new OperationPermissionModel(smpd.MethodName, smpd.Permission,
                    smpd.UserType, spd.SystemModule, smpd.OtherParams));
            }

            return new AuthorizePermissionModel(spd.ServiceName, spd.ServiceId, spd.Tag, 0, spd.SystemModule,
                spd.OtherParams, operationPermissionModels, null);
        }).ToList();

        return Task.FromResult(list);
    }

    public Task<List<AuthorizeDataRuleModel>> GetAuthorizeDataRuleList()
    {
        var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();

        var entities = typeFinder.FindOfType<Entity>();
        var authorizeDataRuleList = new List<AuthorizeDataRuleModel>();


        foreach (var entity in entities)
        {
            var entityDataRuleAttribute = entity.GetCustomAttribute<DataRuleAttribute>();

            if (entityDataRuleAttribute == null) continue;

            var model = new AuthorizeDataRuleModel(
                entityDataRuleAttribute.AttributeDesc,
                entity.FullName,
                entityDataRuleAttribute.Tag,
                entityDataRuleAttribute.Order,
                new List<AuthorizeDataRuleFieldModel>()
            );

            var properties = entity.GetProperties();

            foreach (var property in properties)
            {
                var propertyDataRuleAttribute = property.GetCustomAttribute<DataRuleAttribute>();
                if (propertyDataRuleAttribute == null) continue;

                model.AuthorizeDataRuleFieldModels.Add(
                    new AuthorizeDataRuleFieldModel(
                        propertyDataRuleAttribute.UserType,
                        property.Name,
                        propertyDataRuleAttribute.AttributeDesc,
                        property.PropertyType.ToString(),
                        string.Empty,
                        ExpressionType.And
                    ));
            }

            authorizeDataRuleList.Add(model);
        }

        return Task.FromResult(authorizeDataRuleList);
    }
}