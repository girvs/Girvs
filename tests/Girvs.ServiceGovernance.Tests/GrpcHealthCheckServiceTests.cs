using Girvs.Grpc;
using Girvs.ServiceGovernance.Services;
using Grpc.Core;
using Grpc.Health.V1;

namespace Girvs.ServiceGovernance.Tests;

public class GrpcHealthCheckServiceTests
{
    [Fact]
    public async Task Check返回Serving并实现Grpc服务标记接口()
    {
        var service = new HealthCheckService();

        var response = await service.Check(new HealthCheckRequest(), null);

        Assert.IsAssignableFrom<IAppGrpcService>(service);
        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, response.Status);
    }

    [Fact]
    public async Task Watch写入一次Serving状态()
    {
        var service = new HealthCheckService();
        var writer = new RecordingServerStreamWriter<HealthCheckResponse>();

        await service.Watch(new HealthCheckRequest(), writer, null);

        var response = Assert.Single(writer.Messages);
        Assert.Equal(HealthCheckResponse.Types.ServingStatus.Serving, response.Status);
    }

    private sealed class RecordingServerStreamWriter<T> : IServerStreamWriter<T>
    {
        public List<T> Messages { get; } = [];

        public WriteOptions WriteOptions { get; set; }

        public Task WriteAsync(T message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
