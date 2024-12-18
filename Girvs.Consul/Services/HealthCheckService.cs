﻿namespace Girvs.Consul.Services;

public class HealthCheckService : Health.HealthBase, IAppGrpcService
{
    public override Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext context)
    {
        //TODO:检查逻辑
        return Task.FromResult(new HealthCheckResponse()
            {Status = HealthCheckResponse.Types.ServingStatus.Serving});
    }

    public override async Task Watch(HealthCheckRequest request,
        IServerStreamWriter<HealthCheckResponse> responseStream, ServerCallContext context)
    {
        //TODO:检查逻辑
        await responseStream.WriteAsync(new HealthCheckResponse()
            {Status = HealthCheckResponse.Types.ServingStatus.Serving});
    }
}