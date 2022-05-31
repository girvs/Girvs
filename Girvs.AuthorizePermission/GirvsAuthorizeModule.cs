using System.Text;
using Girvs.AuthorizePermission.Configuration;
using Girvs.Infrastructure;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Girvs.AuthorizePermission
{
    public class GirvsAuthorizeModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var authorizeConfig = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();

            var authenticationBuilder =
                services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme);

            if ((authorizeConfig.AuthorizationModel & AuthorizationModel.Jwt) == AuthorizationModel.Jwt)
            {
                authenticationBuilder
                    .AddJwtBearer(GirvsAuthenticationScheme.GirvsJwt, x =>
                    {
                        //使用应用密钥得到一个加密密钥字节数组
                        var key = Encoding.ASCII.GetBytes(authorizeConfig.JwtConfig.Secret);
                        x.RequireHttpsMetadata = true;
                        x.SaveToken = true;
                        x.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = false,
                            ValidateAudience = false
                        };
                        x.Events = new JwtBearerEvents();
                    });
            }

            if ((authorizeConfig.AuthorizationModel & AuthorizationModel.IdentityServer4) ==
                AuthorizationModel.IdentityServer4)
            {
                authenticationBuilder
                    .AddJwtBearer(GirvsAuthenticationScheme.GirvsIdentityServer4, x =>
                    {
                        //使用应用密钥得到一个加密密钥字节数组
                        var key = Encoding.ASCII.GetBytes(authorizeConfig.IdentityServer4Config.ApiSecret +
                                                          authorizeConfig.IdentityServer4Config.ApiSecret);
                        x.RequireHttpsMetadata = true;
                        x.SaveToken = true;
                        x.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = false,
                            ValidateAudience = false
                        };
                        x.Events = new JwtBearerEvents();
                    });
                // authenticationBuilder
                //     .AddIdentityServerAuthentication(GirvsAuthenticationScheme.GirvsIdentityServer4, options =>
                //     {
                //         //由于名称过长，暂时替换
                //         options.Authority = authorizeConfig.IdentityServer4Config.ServerHost;
                //         options.RequireHttpsMetadata = authorizeConfig.IdentityServer4Config.UseHttps;
                //         options.SupportedTokens = SupportedTokens.Both;
                //         options.ApiSecret = authorizeConfig.IdentityServer4Config.ApiSecret;
                //         options.ApiName = authorizeConfig.IdentityServer4Config.ApiResourceName;
                //         options.IntrospectionDiscoveryPolicy.RequireKeySet = false;
                //     });
            }
        }

        public void Configure(IApplicationBuilder application)
        {
            application.UseAuthentication();
            application.UseAuthorization();
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 99905;
    }
}