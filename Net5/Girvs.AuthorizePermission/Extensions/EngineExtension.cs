using System;
using Girvs.AuthorizePermission.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;

namespace Girvs.AuthorizePermission.Extensions
{
    public static class EngineExtension
    {
        private static ClaimValueConfig GetClaimValueConfig(IEngine engine)
        {
            return Singleton<AppSettings>.Instance[nameof(AuthorizeConfig)].ClaimValueConfig;
        }

        public static Guid? GetUserId(this IEngine engine)
        {
            var claimConfig = GetClaimValueConfig(engine);

            var userId = engine.GetCurrentClaimByName(claimConfig.ClaimSid).Value;

            if (string.IsNullOrEmpty(userId) && Guid.Empty.ToString() == userId)
            {
                return null;
            }

            return Guid.Parse(userId);
        }


        public static string GetUserName(this IEngine engine)
        {
            var claimConfig = GetClaimValueConfig(engine);

            return engine.GetCurrentClaimByName(claimConfig.ClaimName).Value;
        }

        public static Guid? GetTenantId(this IEngine engine)
        {
            var claimConfig = GetClaimValueConfig(engine);

            var tenantId = engine.GetCurrentClaimByName(claimConfig.ClaimTenantId).Value;

            if (string.IsNullOrEmpty(tenantId) && Guid.Empty.ToString() == tenantId)
            {
                return null;
            }

            return Guid.Parse(tenantId);
        }

        public static string GetTenantName(this IEngine engine)
        {
            var claimConfig = GetClaimValueConfig(engine);

            return engine.GetCurrentClaimByName(claimConfig.ClaimTenantName).Value;
        }
    }
}