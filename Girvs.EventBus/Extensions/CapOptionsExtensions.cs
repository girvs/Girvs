namespace Girvs.EventBus.Extensions;

public static class CapOptionsExtensions
{
    public static void AddCapSubscribe(this IServiceCollection services)
    {
        var typeFinder = new WebAppTypeFinder();
        var subscribes = typeFinder.FindOfType<ICapSubscribe>();

        foreach (var subscribe in subscribes)
        {
            services.AddScoped(subscribe);
        }
    }
}