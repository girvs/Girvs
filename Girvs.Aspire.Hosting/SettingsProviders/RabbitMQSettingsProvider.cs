using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting.SettingsProviders;

/// <summary>内置预设:RabbitMQ 容器资源 → Type=rabbitmq,Settings.HostName/Port/UserName/Password/VirtualHost。</summary>
public class RabbitMQSettingsProvider : IGirvsResourceSettingsProvider
{
    public virtual async Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
    {
        if (resource is not RabbitMQServerResource rabbit)
            return null;

        var (host, port) = rabbit.PrimaryEndpoint();
        return new GirvsResource
        {
            Type = "rabbitmq",
            Settings = new Dictionary<string, string>
            {
                ["HostName"] = host,
                ["Port"] = port.ToString(),
                ["UserName"] = rabbit.UserNameParameter is null
                    ? "guest"
                    : await rabbit.UserNameParameter.GetValueAsync(ct),
                ["Password"] = await rabbit.PasswordParameter.GetValueAsync(ct),
                ["VirtualHost"] = "/",
            },
        };
    }
}
