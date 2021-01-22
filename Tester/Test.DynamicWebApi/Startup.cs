using System;
using Girvs.WebFrameWork.Plugins;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Test.DynamicWebApi
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
            services.ConfigureApplicationServices(Configuration, WebHostEnvironment);
            
            services//.AddAuthorization()
                .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    var audienceName = AppDomain.CurrentDomain.FriendlyName.Replace(".", "_");
                    options.Authority = "http://192.168.1.199:1902";
                    options.RequireHttpsMetadata = false;
                    options.SupportedTokens = SupportedTokens.Both;
                    options.ApiSecret = "bw5DgwD9p7ltQf5k";
                    options.ApiName = "Power_Location_WebApi";
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (WebHostEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseGirvsExceptionHandler();
            app.ConfigureRequestPipeline();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.ConfigureEndpointRouteBuilder();
            });
        }
    }
}
