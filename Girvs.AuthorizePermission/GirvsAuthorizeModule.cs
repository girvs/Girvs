using System;
using System.Text;
using System.Threading.Tasks;
using Girvs.AuthorizePermission.Configuration;
using Girvs.Configuration;
using Girvs.Extensions;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
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
                        x.Events = TransferHeadToken();
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
                        x.Events = TransferHeadToken();
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

        private JwtBearerEvents TransferHeadToken()
        {
           return new JwtBearerEvents()
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.HttpContext.Request.Query["access_token"].ToString();
                    if (!accessToken.IsNullOrEmpty())
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
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