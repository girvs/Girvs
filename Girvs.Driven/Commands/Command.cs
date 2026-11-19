namespace Girvs.Driven.Commands;

public abstract record Command<TResponse>(
    string CommandDesc,
    string OperateIpAddress,
    DateTime Timestamp,
    ValidationResult ValidationResult
) : Message<TResponse>
{
    // 本机 IP 解析结果缓存：嵌套非泛型静态类保证全进程仅解析一次
    //（直接放在泛型记录上会随每个封闭类型重复解析）
    private static class LocalIpAddressCache
    {
        // Lazy 默认线程安全；解析失败时返回 null，确保 .Value 不会抛出
        public static readonly Lazy<string> Cached = new(Resolve);

        private static string Resolve()
        {
            try
            {
                return System
                    .Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
                    .AddressList.FirstOrDefault(address =>
                        address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    )
                    ?.ToString();
            }
            catch
            {
                // DNS 解析失败时降级为 null，不影响命令构造
                return null;
            }
        }
    }

    public Command(string commandDesc)
        : this(commandDesc, null, DateTime.Now, null)
    {
        if (EngineContext.Current.HttpContext != null)
        {
            OperateIpAddress =
                EngineContext.Current.HttpContext.Request.GetApiGateWayRemoteIpAddress();
        }
        else
        {
            OperateIpAddress = LocalIpAddressCache.Cached.Value;
        }
    }

    public virtual void AddFluentValidationRule<TCommand>(AbstractValidator<TCommand> validator) { }

    public virtual bool IsValid()
    {
        return true;
    }
}

public abstract record Command(
    string CommandDesc,
    string OperateIpAddress,
    DateTime Timestamp,
    ValidationResult ValidationResult
) : Command<bool>(CommandDesc, OperateIpAddress, Timestamp, ValidationResult)
{
    public Command(string commandDesc)
        : this(commandDesc, null, DateTime.Now, null) { }
}
