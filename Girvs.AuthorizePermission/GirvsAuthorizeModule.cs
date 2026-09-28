using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Girvs.AuthorizePermission.Middleware;

namespace Girvs.AuthorizePermission;

public class GirvsAuthorizeModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var authorizeConfig = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();

        var authenticationBuilder = services.AddAuthentication("Bearer");

        if ((authorizeConfig.AuthorizationModel & AuthorizationModel.Jwt) == AuthorizationModel.Jwt)
        {
            authenticationBuilder.AddJwtBearer(
                GirvsAuthenticationScheme.GirvsJwt,
                x =>
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
                }
            );
        }

        if (
            (authorizeConfig.AuthorizationModel & AuthorizationModel.JwtWebFront)
            == AuthorizationModel.JwtWebFront
        )
        {
            authenticationBuilder.AddJwtBearer(
                GirvsAuthenticationScheme.GirvsJwtWebFront,
                x =>
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
                }
            );
        }

        if (
            (authorizeConfig.AuthorizationModel & AuthorizationModel.OAuth2)
            == AuthorizationModel.OAuth2
        )
        {
            authenticationBuilder.AddJwtBearer(
                GirvsAuthenticationScheme.GirvsOAuth2,
                options =>
                {
                    options.Authority = authorizeConfig.OAuth2Config.Authority;
                    options.Audience = authorizeConfig.OAuth2Config.Audience;
                    options.RequireHttpsMetadata = authorizeConfig.OAuth2Config.RequireHttpsMetadata;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = authorizeConfig
                            .OAuth2Config
                            .ValidateIssuerSigningKey,
                        ValidateIssuer = authorizeConfig.OAuth2Config.ValidateIssuer,
                        ValidateAudience = authorizeConfig.OAuth2Config.ValidateAudience,
                    };
                }
            );
        }
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
        application.UseAuthentication();
        application.UseAuthorization();
        application.UseMiddleware<GirvsTenantClaimsMiddleware>();
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = 99905;
}
