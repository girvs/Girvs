using System;
using Microsoft.AspNetCore.Builder;

namespace Girvs.WebFrameWork.Plugins.Swagger
{
    public static class SwaggerApplicationExtensions
    {
        public static IApplicationBuilder UseSwaggerService(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", AppDomain.CurrentDomain.FriendlyName);
            });
            return app; 
        }
    }
}