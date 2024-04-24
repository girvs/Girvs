using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Girvs.AuthorizePermission;

public class GirvsAuthorizeModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var authorizeConfig = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();

        var authenticationBuilder =
            services.AddAuthentication("Bearer");

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
        
        if ((authorizeConfig.AuthorizationModel & AuthorizationModel.JwtWebFront) == AuthorizationModel.JwtWebFront)
        {
            authenticationBuilder
                .AddJwtBearer(GirvsAuthenticationScheme.GirvsJwtWebFront, x =>
                {
                    //使用应用密钥得到一个加密密钥字节数组
                    var key = Encoding.ASCII.GetBytes(authorizeConfig.JwtWebFrontConfig.Secret);
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
                .AddJwtBearer(GirvsAuthenticationScheme.GirvsIdentityServer4, options =>
                {
                    options.Authority = authorizeConfig.IdentityServer4Config.ServerHost;
                    options.Audience = authorizeConfig.IdentityServer4Config.ApiResourceName;
                    options.RequireHttpsMetadata = authorizeConfig.IdentityServer4Config.UseHttps;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = authorizeConfig.IdentityServer4Config.ValidateIssuerSigningKey,
                        ValidateIssuer = authorizeConfig.IdentityServer4Config.ValidateIssuer,
                        ValidateAudience = authorizeConfig.IdentityServer4Config.ValidateAudience,
                        SignatureValidator = (token, _) => new JsonWebToken(token),
                    };
                });
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