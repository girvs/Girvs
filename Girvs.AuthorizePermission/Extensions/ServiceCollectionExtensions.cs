namespace Girvs.AuthorizePermission.Extensions;

public static class ServiceCollectionExtensions
{
    public static IMvcBuilder AddControllersWithAuthorizePermissionFilter(this IServiceCollection services)
    {
        return services.AddControllersWithAuthorizePermissionFilter(option =>
        {
            option.Filters.Add<ActionPermissionFilter>();
        });
    }
        
        
    public static IMvcBuilder AddControllersWithAuthorizePermissionFilter(this IServiceCollection services,Action<MvcOptions> configure)
    {
        return services.AddControllers(option =>
        {
            option.Filters.Add<ActionPermissionFilter>();
            configure(option);
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter());
        } );
    }
}