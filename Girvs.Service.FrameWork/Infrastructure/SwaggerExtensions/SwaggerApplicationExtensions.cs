using System;
using Microsoft.AspNetCore.Builder;

namespace Girvs.Service.FrameWork.Infrastructure.SwaggerExtensions
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