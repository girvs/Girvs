

namespace Girvs.Aspire.Hosting.SettingsProviders;

/// <summary>内置预设:Kafka 容器资源 → Type=kafka,Settings.BootstrapServers。</summary>
public class KafkaSettingsProvider : IGirvsResourceSettingsProvider
{
    public virtual Task<GirvsInfrastructureResource> TryBuildAsync(IResource resource, CancellationToken ct)
    {
        if (resource is not KafkaServerResource kafka)
            return Task.FromResult<GirvsInfrastructureResource>(null);

        var (host, port) = kafka.PrimaryEndpoint();
        return Task.FromResult(
            new GirvsInfrastructureResource
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
