using System;
using System.Security.Claims;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Infrastructure;

namespace Girvs.AuthorizePermission.Extensions
{
    public static class GirvsClaimManagerExtensions
    {
        public static string GirvsIdentityUserTypeClaimTypes = ClaimTypes.NameIdentifier;
        
        public static UserType GetUserType(this IGirvsClaimManager claimManager)
        {
            var userTypeStr = claimManager.IdentityClaim[GirvsIdentityUserTypeClaimTypes];
            if (string.IsNullOrEmpty(userTypeStr))
            {
                return UserType.All;
            }

            return (UserType) Enum.Parse(typeof(UserType), userTypeStr);
        }
    }
}