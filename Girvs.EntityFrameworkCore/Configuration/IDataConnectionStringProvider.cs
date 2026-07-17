using Girvs.Configuration.Resources;

namespace Girvs.EntityFrameworkCore.Configuration;

public interface IDataConnectionStringProvider
{
    string GetMasterConnectionString(string name);
    string GetReadConnectionString(string name);
}

public sealed class DataConnectionStringProvider : IDataConnectionStringProvider
{
    private readonly Dictionary<string, (string Master, IReadOnlyList<string> Reads)> _connections = new();

    public DataConnectionStringProvider(
        IEnumerable<DataConnectionConfig> configurations,
        IReadOnlyDictionary<string, Resource> resources,
        IConfiguration configuration
    )
    {
        foreach (var config in configurations)
        {
            var master = GetResource(resources, config.ConnectionRef);
            var masterConnection = configuration?.GetConnectionString($"girvs-db-{config.Name}")
                ?? config.BuildConnectionString(master);
            var reads = config.ReadConnectionRefs
                .Select((reference, index) => configuration?.GetConnectionString($"girvs-db-{config.Name}-read-{index}")
                    ?? config.BuildConnectionString(GetResource(resources, reference)))
                .ToList();
            _connections[config.Name] = (masterConnection, reads);
        }
    }

    public string GetMasterConnectionString(string name) => _connections[name].Master;

    public string GetReadConnectionString(string name)
    {
        var connection = _connections[name];
        if (connection.Reads.Count == 0) return connection.Master;
        return connection.Reads[SecureRandomNumberGenerator.GetInt32(0, connection.Reads.Count)];
    }

    private static Resource GetResource(IReadOnlyDictionary<string, Resource> resources, string name) =>
        resources.TryGetValue(name, out var resource)
            ? resource
            : throw new GirvsException($"Resources:{name} 未配置");
}
