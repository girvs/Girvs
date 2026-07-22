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
        IReadOnlyDictionary<string, GirvsInfrastructureResource> resources
    )
    {
        foreach (var config in configurations)
        {
            var master = GetResource(resources, config.ConnectionRef);
            var masterConnection = config.BuildConnectionString(master);
            var reads = config.ReadConnectionRefs
                .Select(reference => config.BuildConnectionString(GetResource(resources, reference)))
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

    private static GirvsInfrastructureResource GetResource(IReadOnlyDictionary<string, GirvsInfrastructureResource> resources, string name) =>
        resources.TryGetValue(name, out var resource)
            ? resource
            : throw new GirvsException($"Resources:{name} 未配置");
}
