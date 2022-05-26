using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Girvs.AuthorizePermission.Configuration;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace Girvs.AuthorizePermission.Extensions
{
    public static class JwtBearerAuthenticationExtension
    {
        public static string GenerateToken(
            string userId,
            string userName,
            string tenantId = null,
            string tenantName = null,
            UserType userType = UserType.All,
            IdentityType identityType = IdentityType.ManagerUser,
            SystemModule claimSystemModule = SystemModule.All)
        {
            var girvsIdentityClaim = new GirvsIdentityClaim()
            {
                UserId = userId,
                UserName = userName,
                TenantId = tenantId,
                TenantName = tenantName,
                IdentityType = identityType,
                SystemModule = claimSystemModule,
                OtherClaims = new Dictionary<string, string>()
                {
                    {GirvsClaimManagerExtensions.GirvsIdentityUserTypeClaimTypes, userType.ToString()}
                }
            };
            return GenerateToken(girvsIdentityClaim);
        }

        public static string GenerateToken(GirvsIdentityClaim girvsIdentityClaim)
        {
            var claimsIdentity = EngineContext.Current.ClaimManager.BuildClaimsIdentity(girvsIdentityClaim);
            return GetJwtAccessToken(claimsIdentity);
        }

        public static string GetJwtAccessToken(ClaimsIdentity claimsIdentity)
        {
            var authorizeConfig = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(authorizeConfig.JwtConfig.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddHours(authorizeConfig.JwtConfig.ExpiresHours),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}