using Serilog;

namespace Girvs.ServiceGovernance.Tests;

[Collection(OtlpEnvironmentVariableCollection.Name)]
public class GirvsSerilogOtlpHookTests
{
    [Fact]
    public void 反射契约_核心能通过类型全名找到AddOtlpSink方法()
    {
        var hookType = Type.GetType(
            "Girvs.ServiceGovernance.GirvsSerilogOtlpHook, Girvs.ServiceGovernance"
        );

        Assert.NotNull(hookType);
        var method = hookType.GetMethod("AddOtlpSink", new[] { typeof(LoggerConfiguration) });
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void 未设置OTLP端点环境变量时不追加Sink也不抛异常()
    {
        var original = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            var loggerConfiguration = new LoggerConfiguration();

            var exception = Record.Exception(
                () => GirvsSerilogOtlpHook.AddOtlpSink(loggerConfiguration)
            );

            Assert.Null(exception);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", original);
        }
    }

    [Fact]
    public void 设置OTLP端点环境变量时追加Sink不抛异常()
    {
        var original = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable(
                "OTEL_EXPORTER_OTLP_ENDPOINT",
                "http://localhost:4317"
            );
            var loggerConfiguration = new LoggerConfiguration();

            var exception = Record.Exception(
                () => GirvsSerilogOtlpHook.AddOtlpSink(loggerConfiguration)
            );

            Assert.Null(exception);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", original);
        }
    }
}
