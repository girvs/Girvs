using System;
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
        public static string GenerateToken(string accessid, string accessName, string tenantId, string tenantName)
        {
            return GenerateToken(accessid, accessName, tenantId, tenantName, UserType.All);
        }

        public static string GenerateToken(string accessid, string accessName, string tenantId, string tenantName,
            UserType userType)
        {
            return GenerateToken(accessid, accessName, tenantId, tenantName, UserType.All, IdentityType.ManagerUser);
        }

        public static string GenerateToken(string accessid, string accessName, string tenantId, string tenantName,
            UserType userType, IdentityType identityType)
        {
            var claimsIdentity =
                EngineContext.Current.ClaimManager.GenerateClaimsIdentity(accessid, accessName, tenantId, tenantName,
                    userType, identityType);

            //在登陆认证成功后，设置当前为登陆
            EngineContext.Current.ClaimManager.CurrentClaims = claimsIdentity.Claims;

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