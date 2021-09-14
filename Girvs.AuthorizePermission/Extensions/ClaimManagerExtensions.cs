using System;
using System.Linq;
using System.Security.Claims;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Infrastructure;

namespace Girvs.AuthorizePermission.Extensions
{
    public static class ClaimManagerExtensions
    {
        public const string ClamisUserTypeName = "GirvsUserType";

        public static ClaimsIdentity GenerateClaimsIdentity(
            this IClaimManager claimManager,
            string sid,
            string name,
            string tenantId,
            string tenantName,
            UserType userType = UserType.All,
            IdentityType identityType = IdentityType.ManagerUser)
        {
            var claimsIdentity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Sid, sid),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.GroupSid, tenantId ?? string.Empty),
                new Claim(ClaimTypes.GivenName, tenantName ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, identityType.ToString()),
                new Claim(ClamisUserTypeName, userType.ToString())
            });

            return claimsIdentity;
        }

        public static UserType GetUserType(this IClaimManager claimManager)
        {
            var claim = claimManager.CurrentClaims?.FirstOrDefault(x => x.Type == ClamisUserTypeName);
            var userTypeStr = claim?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(userTypeStr))
            {
                return UserType.All;
            }

            return (UserType) Enum.Parse(typeof(UserType), userTypeStr);
        }
    }
}