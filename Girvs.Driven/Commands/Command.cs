namespace Girvs.Driven.Commands;

public abstract record Command<TResponse>(string CommandDesc, string OperateIpAddress, DateTime Timestamp,
    ValidationResult ValidationResult) : Message<TResponse>
{
    public Command(string commandDesc) : this(commandDesc, null, DateTime.Now, null)
    {
        if (EngineContext.Current.HttpContext != null)
        {
            OperateIpAddress = EngineContext.Current.HttpContext.Request.GetApiGateWayRemoteIpAddress();
        }
        else
        {
            var addressList = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
            OperateIpAddress = addressList
                .FirstOrDefault(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?.ToString();
        }
    }

    public virtual void AddFluentValidationRule<TCommand>(AbstractValidator<TCommand> validator)
    {
    }

    public virtual bool IsValid()
    {
        return true;
    }
}

public abstract record Command(string CommandDesc, string OperateIpAddress, DateTime Timestamp,
    ValidationResult ValidationResult) : Command<bool>(CommandDesc, OperateIpAddress, Timestamp, ValidationResult)
{
    public Command(string commandDesc) : this(commandDesc, null, DateTime.Now, null)
    {
    }
}