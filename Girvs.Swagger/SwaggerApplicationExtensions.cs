using IGeekFan.AspNetCore.Knife4jUI;

namespace Girvs.Swagger;

public static class SwaggerApplicationExtensions
{
    public static IApplicationBuilder UseSwaggerService(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", AppDomain.CurrentDomain.FriendlyName);
        });
        app.UseKnife4UI(c =>
        {
            c.RoutePrefix = ""; // serve the UI at root
            c.SwaggerEndpoint("/v1/api-docs", "V1 Docs");
        });
        return app; 
    }
}