namespace Girvs.EventBus.Tests;

public class EventBusModuleTests
{
    [Theory]
    [InlineData("sqlserver")]
    [InlineData("mysql")]
    [InlineData("sqlite")]
    public void ValidatePersistenceTypeSupported_已知持久化类型_不抛异常(string persistenceType)
    {
        EventBusModule.ValidatePersistenceTypeSupported(persistenceType);
    }

    [Fact]
    public void ValidatePersistenceTypeSupported_未知持久化类型_抛GirvsException并携带类型名()
    {
        var exception = Assert.Throws<GirvsException>(
            () => EventBusModule.ValidatePersistenceTypeSupported("oracle"));

        Assert.Contains("oracle", exception.Message);
    }

    [Theory]
    [InlineData("rabbitmq")]
    [InlineData("kafka")]
    [InlineData("redis")]
    public void ValidateTransportTypeSupported_已知传输类型_不抛异常(string transportType)
    {
        EventBusModule.ValidateTransportTypeSupported(transportType);
    }

    [Fact]
    public void ValidateTransportTypeSupported_未知传输类型_抛GirvsException并携带类型名()
    {
        var exception = Assert.Throws<GirvsException>(
            () => EventBusModule.ValidateTransportTypeSupported("activemq"));

        Assert.Contains("activemq", exception.Message);
    }
}
