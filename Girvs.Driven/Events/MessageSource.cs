namespace Girvs.Driven.Events;

public record MessageSource(string SourceName, string IpAddress, string SourceNameId, string TenantId,
    string TenantName);