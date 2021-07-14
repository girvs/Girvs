using System;
using Girvs.AuthorizePermission.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;

namespace Girvs.AuthorizePermission.Extensions
{
    public static class EngineExtension
    {
        public static Guid? GetUserId(this IEngine engine)
        {
            var appsettings = engine.Resolve<AppSettings>();
            var claimConfig = appsettings.ModuleConfigurations[nameof(ClaimValueConfig)] as ClaimValueConfig ??
                              throw new ArgumentNullException(nameof(ClaimValueConfig));

            var userId = engine.GetCurrentClaimByName(claimConfig.ClaimSid).Value;

            if (string.IsNullOrEmpty(userId) && Guid.Empty.ToString() == userId)
            {
                return null;
            }

            return Guid.Parse(userId);
        }

        public static string GetUserName(this IEngine engine)
        {
            var appsettings = engine.Resolve<AppSettings>();
            var claimConfig = appsettings.ModuleConfigurations[nameof(ClaimValueConfig)] as ClaimValueConfig ??
                              throw new ArgumentNullException(nameof(ClaimValueConfig));

            return engine.GetCurrentClaimByName(claimConfig.ClaimName).Value;
        }

        public static Guid? GetTenantId(this IEngine engine)
        {
            var appsettings = engine.Resolve<AppSettings>();
            var claimConfig = appsettings.ModuleConfigurations[nameof(ClaimValueConfig)] as ClaimValueConfig ??
                              throw new ArgumentNullException(nameof(ClaimValueConfig));

            var tenantId = engine.GetCurrentClaimByName(claimConfig.ClaimTenantId).Value;

            if (string.IsNullOrEmpty(tenantId) && Guid.Empty.ToString() == tenantId)
            {
                return null;
            }

            return Guid.Parse(tenantId);
        }
        
        public static string GetTenantName(this IEngine engine)
        {
            var appsettings = engine.Resolve<AppSettings>();
            var claimConfig = appsettings.ModuleConfigurations[nameof(ClaimValueConfig)] as ClaimValueConfig ??
                              throw new ArgumentNullException(nameof(ClaimValueConfig));

            return engine.GetCurrentClaimByName(claimConfig.ClaimTenantName).Value;
        }
    }
}