using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Power.BasicManagement.Domain.Models;
using Power.BasicManagement.Domain.Repositories;

namespace Power.BasicManagement.Domain.Extensions
{
    public static class EngineContextExtensions
    {
        public static async Task<User> GetCurrentUser(this IEngine engine)
        {
            var config = engine.Resolve<GirvsConfig>();
            if (config != null)
            {
                Guid userId = GetUserId(config) ?? throw new ArgumentNullException($"GetUserId is empty");
                var userRepository = EngineContext.Current.Resolve<IUserRepository>();
                return await userRepository.GetUserByIdIncludeRolesAsync(userId);
            }
            throw new ArgumentNullException($"unRead config");
        }

        private static Guid? GetOrganizationId(GirvsConfig config)
        {
            var claims = EngineContext.Current.HttpContext.User.Claims;

            var idStr = claims.FirstOrDefault(x => x.Type == config.ClaimValueConfig.ClaimTenantId)?.Value;

            if (!string.IsNullOrEmpty(idStr))
            {
                return Guid.Parse(idStr);
            }

            return null;
        }

        private static Guid? GetUserId(GirvsConfig config)
        {
            var claims = EngineContext.Current.HttpContext.User.Claims;

            var idStr = claims.FirstOrDefault(x => x.Type == config.ClaimValueConfig.ClaimSid)?.Value;

            if (!string.IsNullOrEmpty(idStr))
            {
                return Guid.Parse(idStr);
            }

            return null;
        }
    }
}