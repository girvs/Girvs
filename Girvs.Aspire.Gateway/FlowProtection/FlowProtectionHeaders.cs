namespace Girvs.Aspire.Gateway.FlowProtection;

public static class FlowProtectionHeaders
{
    public const string Ticket = "X-Flow-Ticket";

    public const string NextIndex = "X-Flow-Next-Index";

    public const string Completed = "X-Flow-Completed";

    public const string BusinessId = "X-Business-Id";

    public const string BusinessIdFromResponse = "X-Flow-Business-Id";

    public static readonly string[] ResponseHeaders =
    [
        Ticket,
        NextIndex,
        Completed,
    ];
}
