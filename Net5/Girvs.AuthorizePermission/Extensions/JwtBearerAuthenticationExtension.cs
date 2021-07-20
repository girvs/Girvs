using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Girvs.AuthorizePermission.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Girvs.AuthorizePermission.Extensions
{
    public static class JwtBearerAuthenticationExtension
    {
        public static string GenerateToken(string accessid, string accessName, string tenantId, string tenantName)
        {
            var claimConfig = Singleton<AppSettings>.Instance[nameof(ClaimValueConfig)] as ClaimValueConfig;

            var claimsIdentity = new ClaimsIdentity(new Claim[]
            {
                new Claim(claimConfig.ClaimSid, accessid),
                new Claim(claimConfig.ClaimName, accessName),
                new Claim(claimConfig.ClaimTenantId, tenantId),
                new Claim(claimConfig.ClaimTenantName, tenantName)
            });

            return GetJwtAccessToken(claimsIdentity);
        }

        public static string GetJwtAccessToken(ClaimsIdentity claimsIdentity)
        {
            var claimConfig = Singleton<AppSettings>.Instance[nameof(ClaimValueConfig)] as ClaimValueConfig;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(claimConfig.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddHours(10),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static void AddJwtBearerAuthentication(this IServiceCollection services)
        {
            var claimConfig = Singleton<AppSettings>.Instance[nameof(ClaimValueConfig)] as ClaimValueConfig;

            if (claimConfig.EnableGirvsAuthorize)
            {
                //使用应用密钥得到一个加密密钥字节数组
                var key = Encoding.ASCII.GetBytes(claimConfig.Secret);
                services.AddAuthentication(x =>
                    {
                        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddCookie(cfg => cfg.SlidingExpiration = true)
                    .AddJwtBearer(x =>
                    {
                        x.RequireHttpsMetadata = true;
                        x.SaveToken = true;
                        x.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = false,
                            ValidateAudience = false
                        };
                    });
            }
        }
    }
}