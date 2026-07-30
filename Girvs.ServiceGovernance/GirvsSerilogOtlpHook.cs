using Serilog;

namespace Girvs.ServiceGovernance;

/// <summary>供 Girvs 核心反射调用的 OTLP 日志 Hook 兼容入口。</summary>
public static class GirvsSerilogOtlpHook
{
    public static void AddOtlpSink(LoggerConfiguration loggerConfiguration) =>
        Logging.GirvsSerilogOtlpHook.AddOtlpSink(loggerConfiguration);
}
