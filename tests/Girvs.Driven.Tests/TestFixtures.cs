namespace Girvs.Driven.Tests;

/// <summary>
/// 用于测试的命令模型，继承 Command&lt;bool&gt; 以覆盖命令构造逻辑
/// </summary>
public record FakeCommand(string Desc) : Command<bool>(Desc);

/// <summary>
/// 响应为 string 的命令模型，用于验证引用类型默认值比较
/// </summary>
public record FakeStringCommand(string Desc) : Command<string>(Desc);

/// <summary>
/// 继承非泛型 Command 的测试命令，用于覆盖 Command 类型分支与 CommandHandler 逻辑
/// </summary>
public record FakePlainCommand(string Desc) : Command(Desc);

/// <summary>
/// 记录事件发布的伪中介总线
/// </summary>
public sealed class FakeMediatorHandler : IMediatorHandler
{
    public List<Event> RaisedEvents { get; } = [];

    public Task<object> SendCommand<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        where TCommand : IBaseRequest
    {
        return Task.FromResult((object)default(bool));
    }

    public Task<TResponse> SendCommand<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        where TCommand : IBaseRequest
    {
        return Task.FromResult(default(TResponse));
    }

    public Task RaiseEvent<T>(
        T @event,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        where T : Event
    {
        RaisedEvents.Add(@event);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Girvs 命令校验器测试替身：Desc 不能为空
/// </summary>
public sealed class FakeGirvsCommandValidator : GirvsCommandValidator<FakeCommand>
{
    public FakeGirvsCommandValidator()
    {
        RuleFor(x => x.Desc).NotEmpty().WithMessage("Desc 不能为空");
    }
}

/// <summary>
/// 非 Girvs 体系的具体校验器：规则恒不通过，用于验证回退语义
/// </summary>
public sealed class PlainFailingValidator : AbstractValidator<FakeCommand>
{
    public PlainFailingValidator()
    {
        RuleFor(x => x.Desc).Equal("__NEVER_MATCH__");
    }
}
