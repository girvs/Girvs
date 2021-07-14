using System;
using Girvs.AuthorizePermission.Configuration;
using Girvs.BusinessBasis.Entities;
using Girvs.Configuration;
using Girvs.Infrastructure;

namespace Girvs.AuthorizePermission.Extensions
{
    public static class BaseEntityExtension
    {
        public static void InitPropertyValue(this BaseEntity entity)
        {
            var appSettings = EngineContext.Current.Resolve<AppSettings>();

            var claimConfig = appSettings.ModuleConfigurations[nameof(ClaimValueConfig)] as ClaimValueConfig ??
                              throw new ArgumentNullException(nameof(ClaimValueConfig));
            
            if (entity is IIncludeCreateTime createTimeObj)
            {
                createTimeObj.CreateTime = DateTime.Now;
            }

            if (entity is IIncludeUpdateTime updateTimeObj)
            {
                updateTimeObj.UpdateTime = DateTime.Now;
            }

            if (entity is IIncludeDeleteField deleteFieldObj)
            {
                deleteFieldObj.IsDelete = false;
            }

            if (entity is IIncludeInitField initFieldObj)
            {
                initFieldObj.IsInitData = false;
            }

            if (entity is IIncludeCreatorName creatorNameObj)
            {
                var creatorName = EngineContext.Current.GetUserName();
                if (!string.IsNullOrEmpty(creatorName))
                {
                    creatorNameObj.CreatorName = creatorName;
                }
            }


            var multiTenantPrperty = entity.GetType().GetProperty("IIncludeMultiTenant");
            if (multiTenantPrperty != null)
            {
                var tenantIdStr = EngineContext.Current.GetCurrentClaimByName(claimConfig.ClaimTenantId)
                    ?.Value;

                if (!string.IsNullOrEmpty(tenantIdStr) && multiTenantPrperty.PropertyType == typeof(Guid))
                {
                    multiTenantPrperty.SetValue(entity, Guid.Parse(tenantIdStr));
                }

                if (!string.IsNullOrEmpty(tenantIdStr) && multiTenantPrperty.PropertyType == typeof(Int32))
                {
                    multiTenantPrperty.SetValue(entity, int.Parse(tenantIdStr));
                }

                if (!string.IsNullOrEmpty(tenantIdStr))
                {
                    multiTenantPrperty.SetValue(entity, tenantIdStr);
                }
            }

            var creatorPrperty = entity.GetType().GetProperty("CreatorId");
            if (creatorPrperty != null)
            {
                var userIdStr = EngineContext.Current.GetCurrentClaimByName(claimConfig.ClaimSid)?.Value;

                if (!string.IsNullOrEmpty(userIdStr) && creatorPrperty.PropertyType == typeof(Guid))
                {
                    creatorPrperty.SetValue(entity, Guid.Parse(userIdStr));
                }

                if (!string.IsNullOrEmpty(userIdStr) && creatorPrperty.PropertyType == typeof(Int32))
                {
                    creatorPrperty.SetValue(entity, int.Parse(userIdStr));
                }

                if (!string.IsNullOrEmpty(userIdStr))
                {
                    creatorPrperty.SetValue(entity, userIdStr);
                }
            }
        }
    }
}