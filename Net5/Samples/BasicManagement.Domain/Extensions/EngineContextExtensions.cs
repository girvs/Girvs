using System;
using System.Linq;
using System.Threading.Tasks;
using BasicManagement.Domain.Models;
using BasicManagement.Domain.Repositories;
using Girvs.Configuration;
using Girvs.Domain.Configuration;
using Girvs.Infrastructure;

namespace BasicManagement.Domain.Extensions
{
    public static class EngineContextExtensions
    {
        public static async Task<User> GetCurrentUser(this IEngine engine)
        {
            var config = engine.Resolve<AppSettings>();
            if (config == null || config.ClaimValueConfig == null) throw new ArgumentNullException($"unRead config");
            var userId = GetUserId(config.ClaimValueConfig) ?? throw new ArgumentNullException($"GetUserId is empty");
            var userRepository = EngineContext.Current.Resolve<IUserRepository>();
            return await userRepository.GetUserByIdIncludeRolesAsync(userId);
        }

        private static Guid? GetOrganizationId(ClaimValueConfig config)
        {
            var claims = EngineContext.Current.HttpContext.User.Claims;

            var idStr = claims.FirstOrDefault(x => x.Type == config.ClaimTenantId)?.Value;

            if (!string.IsNullOrEmpty(idStr))
            {
                return Guid.Parse(idStr);
            }

            return null;
        }

        private static Guid? GetUserId(ClaimValueConfig config)
        {
            var claims = EngineContext.Current.HttpContext.User.Claims;

            var idStr = claims.FirstOrDefault(x => x.Type == config.ClaimSid)?.Value;

            if (!string.IsNullOrEmpty(idStr))
            {
                return Guid.Parse(idStr);
            }

            return null;
        }
    }
}