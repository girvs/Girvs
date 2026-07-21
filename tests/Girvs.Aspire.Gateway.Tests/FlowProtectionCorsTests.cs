using Girvs.Aspire.Gateway.FlowProtection;

namespace Girvs.Aspire.Gateway.Tests;

public class FlowProtectionCorsTests
{
    [Fact]
    public void 默认Cors暴露流程响应Header()
    {
        Assert.Contains("X-Flow-Ticket", FlowProtectionHeaders.ResponseHeaders);
        Assert.Contains("X-Flow-Next-Index", FlowProtectionHeaders.ResponseHeaders);
        Assert.Contains("X-Flow-Completed", FlowProtectionHeaders.ResponseHeaders);
    }
}
