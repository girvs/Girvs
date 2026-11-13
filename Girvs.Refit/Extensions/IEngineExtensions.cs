namespace Girvs.Refit.Extensions;

public static class IEngineExtensions
{
    public static T RestService<T>(this IEngine engine) where T : class =>
        engine.RestServiceAsync<T>().GetAwaiter().GetResult();

    public static Task<T> RestServiceAsync<T>(this IEngine engine) where T : class
    {
        var client = engine.Resolve<T>();
        return client is null
            ? Task.FromException<T>(new GirvsException($"未注册 Refit 客户端 {typeof(T).Name}"))
            : Task.FromResult(client);
    }
}
