using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting.SettingsProviders;

/// <summary>内置预设:Kafka 容器资源 → Type=kafka,Settings.BootstrapServers。</summary>
public class KafkaSettingsProvider : IGirvsResourceSettingsProvider
{
    public virtual Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
    {
        if (resource is not KafkaServerResource kafka)
            return Task.FromResult<GirvsResource>(null);

        var (host, port) = kafka.PrimaryEndpoint();
        return Task.FromResult(
            new GirvsResource
            {
                Type = "kafka",
                Settings = new Dictionary<string, string>
                {
                    ["BootstrapServers"] = $"{host}:{port}",
                },
            }
        );
    }
}
