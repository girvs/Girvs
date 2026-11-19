namespace Girvs.EventBus.Tests;

using CapEventBus = Girvs.EventBus.CapEventBus.CapEventBus;

public class CapEventBusTests
{
    [Fact]
    public async Task PublishAsync_事件为Null_抛出ArgumentNullException()
    {
        var eventBus = CreateEventBus();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => eventBus.PublishAsync<IntegrationEvent>(null));
    }

    [Fact]
    public async Task PublishAsync_合法事件_以事件类型名为主题发布()
    {
        var publisher = new FakeCapPublisher();
        var eventBus = CreateEventBus(publisher);

        await eventBus.PublishAsync(new IntegrationEvent());

        Assert.Equal(nameof(IntegrationEvent), publisher.LastName);
        Assert.NotNull(publisher.LastContent);
    }

    private static CapEventBus CreateEventBus(ICapPublisher capPublisher = null)
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddSingleton<IGirvsPrincipalAccessor, GirvsPrincipalAccessor>();
        var provider = services.BuildServiceProvider();

        return new CapEventBus(
            capPublisher ?? new FakeCapPublisher(),
            NullLogger<CapEventBus>.Instance,
            provider.GetRequiredService<IGirvsPrincipalAccessor>());
    }

    private sealed class FakeCapPublisher : ICapPublisher
    {
        public string LastName { get; private set; }
        public object LastContent { get; private set; }

        public IServiceProvider ServiceProvider => null;

        public ICapTransaction Transaction { get; set; }

        public Task PublishAsync<T>(
            string name,
            T contentObj,
            string callbackName = null,
            CancellationToken cancellationToken = default)
        {
            Record(name, contentObj);
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(
            string name,
            T contentObj,
            IDictionary<string, string> headers,
            CancellationToken cancellationToken = default)
        {
            Record(name, contentObj);
            return Task.CompletedTask;
        }

        public void Publish<T>(string name, T contentObj, string callbackName = null) =>
            Record(name, contentObj);

        public void Publish<T>(string name, T contentObj, IDictionary<string, string> headers) =>
            Record(name, contentObj);

        public Task PublishDelayAsync<T>(
            TimeSpan delayTime,
            string name,
            T contentObj,
            IDictionary<string, string> headers,
            CancellationToken cancellationToken = default)
        {
            Record(name, contentObj);
            return Task.CompletedTask;
        }

        public Task PublishDelayAsync<T>(
            TimeSpan delayTime,
            string name,
            T contentObj,
            string callbackName = null,
            CancellationToken cancellationToken = default)
        {
            Record(name, contentObj);
            return Task.CompletedTask;
        }

        public void PublishDelay<T>(
            TimeSpan delayTime,
            string name,
            T contentObj,
            IDictionary<string, string> headers) =>
            Record(name, contentObj);

        public void PublishDelay<T>(
            TimeSpan delayTime,
            string name,
            T contentObj,
            string callbackName = null) =>
            Record(name, contentObj);

        private void Record(string name, object content)
        {
            LastName = name;
            LastContent = content;
        }
    }
}
