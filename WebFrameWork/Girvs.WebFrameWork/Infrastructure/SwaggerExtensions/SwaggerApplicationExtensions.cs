using System;
using Microsoft.AspNetCore.Builder;

namespace Girvs.WebFrameWork.Infrastructure.SwaggerExtensions
{
    public static class SwaggerApplicationExtensions
    {
        public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
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