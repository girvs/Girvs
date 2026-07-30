using Serilog.Sinks.OpenTelemetry;

namespace Girvs.ServiceGovernance;

/// <summary>
/// 由 Girvs 核心的 HostUseSerilog 通过反射调用。
/// 契约：类型全名 Girvs.ServiceGovernance.GirvsSerilogOtlpHook 与方法签名 AddOtlpSink(LoggerConfiguration) 不可改动，
/// 改动时必须同步修改 GirvsHostBuilderManager.TryAddGirvsOtlpSink。
/// </summary>
public static class GirvsSerilogOtlpHook
{
    public const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static void AddOtlpSink(LoggerConfiguration loggerConfiguration)
    {
        var endpoint = Environment.GetEnvironmentVariable(OtlpEndpointVariable);
        if (string.IsNullOrEmpty(endpoint))
            return;

        loggerConfiguration.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = GetProtocol();

            var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
            if (!string.IsNullOrEmpty(serviceName))
            {
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                };
            }
        });
    }

    private static OtlpProtocol GetProtocol()
    {
        var protocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        return protocol?.ToLowerInvariant() switch
        {
            "http/protobuf" or "http" => OtlpProtocol.HttpProtobuf,
            _ => OtlpProtocol.Grpc,
        };
    }
}
