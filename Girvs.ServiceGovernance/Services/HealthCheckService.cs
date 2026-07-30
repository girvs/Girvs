namespace Girvs.ServiceGovernance.Services;

/// <summary>
/// 标准 gRPC 健康检查服务，由 GrpcModule 自动发现并映射。
/// </summary>
public class HealthCheckService : Health.HealthBase, IAppGrpcService
{
    public override Task<HealthCheckResponse> Check(
        HealthCheckRequest request,
        ServerCallContext context
    ) =>
        Task.FromResult(
            new HealthCheckResponse
            {
                Status = HealthCheckResponse.Types.ServingStatus.Serving,
            }
        );

    public override Task Watch(
        HealthCheckRequest request,
        IServerStreamWriter<HealthCheckResponse> responseStream,
        ServerCallContext context
    ) =>
        responseStream.WriteAsync(
            new HealthCheckResponse
            {
                Status = HealthCheckResponse.Types.ServingStatus.Serving,
            }
        );
}
