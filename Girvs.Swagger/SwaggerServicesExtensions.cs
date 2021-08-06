using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Girvs.Swagger
{
    public static class SwaggerServicesExtensions
    {
        public static void AddSwaggerServices(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = AppDomain.CurrentDomain.FriendlyName, Version = "v1"});
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
                c.DocumentFilter<SwaggerAddEnumDescriptions>();

                var basePath = Path.GetDirectoryName(typeof(SwaggerServicesExtensions).Assembly.Location) ??
                               string.Empty;

                var xmlFiles = Directory.GetFiles(basePath, "*.xml");
                foreach (var xmlFile in xmlFiles)
                {
                    c.IncludeXmlComments(xmlFile);
                }

                var serviceName = AppDomain.CurrentDomain.FriendlyName.Replace(".", "_");
                c.AddServer(new OpenApiServer() {Url = "/", Description = "默认访问"});
                c.AddServer(new OpenApiServer() {Url = "/" + serviceName, Description = "网关访问"});
            });
        }
    }
}