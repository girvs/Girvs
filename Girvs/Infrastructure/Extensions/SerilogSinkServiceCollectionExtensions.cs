using Serilog;

namespace Girvs.Infrastructure.Extensions;

public static class SerilogSinkServiceCollectionExtensions
{
    public static IServiceCollection AddSerilogSink(
        this IServiceCollection services,
        Action<LoggerConfiguration> configure,
        params string[] overriddenSinkTypeNames
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var overrides = overriddenSinkTypeNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        GirvsSerilogSinkRegistry.Register(new SerilogSinkRegistration(configure, overrides));
        return services;
    }
}
