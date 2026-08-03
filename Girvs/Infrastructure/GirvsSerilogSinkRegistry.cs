using Serilog;

namespace Girvs;

internal static class GirvsSerilogSinkRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly List<SerilogSinkRegistration> Registrations = [];

    internal static void Register(SerilogSinkRegistration registration)
    {
        lock (SyncRoot)
        {
            Registrations.RemoveAll(registration.HasSameOverrides);
            Registrations.Add(registration);
        }
    }

    internal static List<SerilogSinkRegistration> SnapshotAndClear()
    {
        lock (SyncRoot)
        {
            var snapshot = new List<SerilogSinkRegistration>(Registrations);
            Registrations.Clear();
            return snapshot;
        }
    }
}

internal sealed record SerilogSinkRegistration(
    Action<LoggerConfiguration> Configure,
    IReadOnlySet<string> OverriddenSinkTypeNames
)
{
    internal bool HasSameOverrides(SerilogSinkRegistration other) =>
        OverriddenSinkTypeNames.SetEquals(other.OverriddenSinkTypeNames);
}
