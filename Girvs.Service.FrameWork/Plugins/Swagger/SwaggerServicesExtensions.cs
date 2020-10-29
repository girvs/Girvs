using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Girvs.Service.FrameWork.Plugins.Swagger
{
    public static class SwaggerServicesExtensions
    {
        public static void AddSwaggerServices(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = AppDomain.CurrentDomain.FriendlyName, Version = "v1" });
                // TODO:一定要返回true！
                c.DocInclusionPredicate((docName, description) => true);

                c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Scheme = "bearer"
                });
                c.OperationFilter<AuthenticationRequirementsOperationFilter>();

                var basePath = Path.GetDirectoryName(typeof(SwaggerServicesExtensions).Assembly.Location) ??
                               string.Empty;
                var xmlPath = Path.Combine(basePath, $"{AppDomain.CurrentDomain.FriendlyName}.xml");
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
        }
    }
}