using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Girvs.AuthorizePermission.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace Girvs.AuthorizePermission.Extensions
{
    public static class JwtBearerAuthenticationExtension
    {
        public static string GenerateToken(string accessid, string accessName, string tenantId, string tenantName)
        {
            var claimsIdentity =
                EngineContext.Current.ClaimManager.GenerateClaimsIdentity(accessid, accessName, tenantId, tenantName);
            return GetJwtAccessToken(claimsIdentity);
        }

        public static string GetJwtAccessToken(ClaimsIdentity claimsIdentity)
        {
            var authorizeConfig = Singleton<AppSettings>.Instance[nameof(AuthorizeConfig)] as AuthorizeConfig;

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