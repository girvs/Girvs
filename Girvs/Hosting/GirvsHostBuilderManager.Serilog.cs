using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace Girvs;

public static partial class GirvsHostBuilderManager
{
    public static void HostUseSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog(
            (context, configuration) =>
            {
                const string outputTemplate =
                    "{Timestamp:yyyy-MM-dd HH:mm:ss} - [{Level:u3}] - {SourceContext:l} - {Message:lj} - {Exception}{NewLine}";

                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .WriteTo.Console(outputTemplate: outputTemplate)
                    .WriteTo.File(
                        Path.Combine(
                            context.HostingEnvironment.ContentRootPath,
                            "logs",
                            "log-.log"
                        ),
                        rollingInterval: RollingInterval.Hour,
                        outputTemplate: outputTemplate,
                        shared: true,
                        rollOnFileSizeLimit: true
                    );

                var registrations = GirvsSerilogSinkRegistry.SnapshotAndClear();
                var overrides = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var registration in registrations)
                {
                    try
                    {
                        registration.Configure(configuration);
                    }
                    catch (Exception exception)
                    {
                        SelfLog.WriteLine(
                            "Girvs Serilog Sink 注册失败 [{0}]: {1}",
                            string.Join(",", registration.OverriddenSinkTypeNames),
                            exception
                        );
                    }

                    overrides.UnionWith(registration.OverriddenSinkTypeNames);
                }

                configuration.ReadFrom.Configuration(
                    BuildFilteredSerilogConfiguration(context.Configuration, overrides)
                );
            }
        );
    }

    private static IConfiguration BuildFilteredSerilogConfiguration(
        IConfiguration configuration,
        ISet<string> overriddenSinkTypeNames
    )
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var serilogSection = configuration.GetSection("Serilog");

        foreach (var section in serilogSection.GetChildren())
        {
            if (string.Equals(section.Key, "WriteTo", StringComparison.OrdinalIgnoreCase))
            {
                var index = 0;
                foreach (var sink in section.GetChildren())
                {
                    if (overriddenSinkTypeNames.Contains(sink["Name"] ?? string.Empty))
                        continue;

                    CopyConfigurationSection(sink, $"Serilog:WriteTo:{index++}", data);
                }
            }
            else
            {
                CopyConfigurationSection(section, $"Serilog:{section.Key}", data);
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static void CopyConfigurationSection(
        IConfigurationSection section,
        string key,
        IDictionary<string, string> data
    )
    {
        var children = section.GetChildren().ToArray();
        if (children.Length == 0)
        {
            data[key] = section.Value;
            return;
        }

        foreach (var child in children)
            CopyConfigurationSection(child, $"{key}:{child.Key}", data);
    }
}
