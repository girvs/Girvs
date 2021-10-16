using Girvs.Extensions;
using Girvs.Infrastructure;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Domain.Extensions
{
    public static class EngineContextExtensions
    {
        public static User GetCurrentUser(this IEngine engine)
        {
            var userId = engine.ClaimManager.GetUserId();
            if (userId.IsNullOrEmpty()) return null;
            var userRepository = engine.Resolve<IUserRepository>();
            return userRepository.GetByIdAsync(userId.ToHasGuid().Value).Result;
        }
    }
}