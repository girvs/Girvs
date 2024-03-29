﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Entities;
using Girvs.DynamicWebApi;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Authorization;
using Panda.DynamicWebApi.Attributes;

namespace Girvs.AuthorizePermission.Services
{
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

                    if (operationPermissionModels.Any(x=>x.OperationName == smpd.MethodName))
                        continue;
                    
                    operationPermissionModels.Add(new OperationPermissionModel()
                    {
                        OperationName = smpd.MethodName,
                        Permission = smpd.Permission,
                        UserType = smpd.UserType,
                        SystemModule = smpd.SystemModule,
                        OtherParams = smpd.OtherParams
                    });
                }

                return new AuthorizePermissionModel
                {
                    Tag = spd.Tag,
                    Order = spd.Order,
                    ServiceName = spd.ServiceName,
                    ServiceId = spd.ServiceId,
                    SystemModule = spd.SystemModule,
                    OtherParams = spd.OtherParams,
                    OperationPermissionModels = operationPermissionModels
                    // Permissions = permissions
                };
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

                var model = new AuthorizeDataRuleModel
                {
                    Tag = entityDataRuleAttribute.Tag,
                    Order = entityDataRuleAttribute.Order,
                    EntityTypeName = entity.FullName,
                    EntityDesc = entityDataRuleAttribute.AttributeDesc
                };

                var properties = entity.GetProperties();

                foreach (var property in properties)
                {
                    var propertyDataRuleAttribute = property.GetCustomAttribute<DataRuleAttribute>();
                    if (propertyDataRuleAttribute == null) continue;

                    model.AuthorizeDataRuleFieldModels.Add(new AuthorizeDataRuleFieldModel()
                    {
                        FieldName = property.Name,
                        FieldType = property.PropertyType.ToString(),
                        FieldDesc = propertyDataRuleAttribute.AttributeDesc,
                        UserType = propertyDataRuleAttribute.UserType
                    });
                }

                authorizeDataRuleList.Add(model);
            }

            return Task.FromResult(authorizeDataRuleList);
        }
    }
}