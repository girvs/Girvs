namespace Girvs.Aspire.Gateway.FlowProtection.Configuration;

/// <summary>
/// 网关流程防护配置。
/// </summary>
public sealed class FlowProtectionOptions
{
    public bool Enabled { get; set; }

    public int DefaultTtlSeconds { get; set; } = 300;

    public int LockTtlSeconds { get; set; } = 30;

    public int CompletedTtlSeconds { get; set; } = 60;

    public string TicketHeaderName { get; set; } = FlowProtectionHeaders.Ticket;

    public string BusinessIdHeaderName { get; set; } = FlowProtectionHeaders.BusinessId;

    public List<FlowDefinitionOptions> Flows { get; set; } = [];
}

public sealed class FlowDefinitionOptions
{
    public string FlowId { get; set; } = string.Empty;

    public int? TtlSeconds { get; set; }

    public List<FlowStepOptions> Steps { get; set; } = [];
}

public sealed class FlowStepOptions
{
    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;
}
