using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Girvs.Domain.Infrastructure;
using Test.Domain.Models;
using Test.Domain.Repositories;

namespace Test.Domain.Extensions
{
    public static class EngineContextExtensions
    {
        public static async Task<User> GetCurrentUser(this IEngine engine)
        {
            Guid userId = GetUserId() ?? throw new ArgumentNullException($"GetUserId is empty");
            var service = EngineContext.Current.Resolve<IUserRepository>();
            return await service.GetByIdAsync(userId);
        }

        private static Guid? GetOrganizationId()
        {
            var claims = EngineContext.Current.HttpContext.User.Claims;

            foreach (var claim in claims)
            {
                if (claim.Type == ClaimTypes.GroupSid)
                {
                    return Guid.Parse(claim.Value);
                }
            }
            return null;
        }

        private static Guid? GetUserId()
        {
            var claims = EngineContext.Current.HttpContext.User.Claims;

            foreach (var claim in claims)
            {
                if (claim.Type == ClaimTypes.Sid)
                {
                    return Guid.Parse(claim.Value);
                }
            }
            return null;
        }
    }
}