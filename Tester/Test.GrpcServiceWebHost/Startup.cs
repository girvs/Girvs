using System.Text;
using Girvs.WebFrameWork.Plugins;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Test.Domain.Configuration;

namespace Test.GrpcServiceWebHost
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private IWebHostEnvironment WebHostEnvironment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            var token = services.ConfigureStartupConfig<TokenManagement>(Configuration.GetSection("tokenConfig"));
            services.ConfigureApplicationServices(Configuration, WebHostEnvironment);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                //Token Validation Parameters
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    //获取或设置要使用的Microsoft.IdentityModel.Tokens.SecurityKey用于签名验证。
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(token.Secret)),
                    //获取或设置一个System.String，它表示将使用的有效发行者检查代币的发行者。
                    ValidIssuer = token.Issuer,
                    //获取或设置一个字符串，该字符串表示将用于检查的有效受众反对令牌的观众。
                    ValidAudience = token.Audience,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                };
            });
            services.AddCors(options =>
            {
                options.AddPolicy("cors",
                    builder =>
                    {
                        string[] headers =
                        {
                            "Grpc-Status",
                            "Grpc-Message",
                            "Grpc-Encoding",
                            "Grpc-Accept-Encoding",
                            "status",
                            //"www-authenticate"
                        };

                        builder.SetIsOriginAllowed(origin => true)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .WithExposedHeaders(headers);
                        //非常重要，直接导致你能否接收到异常信息
                    });
            });
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (WebHostEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            //静态文件
            app.UseStaticFiles();
            app.ConfigureRequestPipeline();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.ConfigureEndpointRouteBuilder();
            });
        }
    }
}