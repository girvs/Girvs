namespace ZhuoFan.Wb.Common.CommandOperater;

public class SendCommandOperaterEvent : ICommandOperateHandler
{
    private readonly IEventBus _eventBus;

    public SendCommandOperaterEvent(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public async Task Handle(Command command, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (!string.IsNullOrEmpty(command.CommandDesc))
        {
            await _eventBus.PublishAsync(new CreateAuditLogEvent()
            {
                CancellationToken = cancellationToken,
                SourceType = EngineContext.Current.ClaimManager.GetIdentityType() == IdentityType.ManagerUser
                    ? SourceType.Server
                    : SourceType.Register,
                AddressIp = command.MessageSource.IpAddress ?? "localhost",
                TenantId = ConvertoGuid(command.MessageSource.TenantId),
                CreatorId = ConvertoGuid(command.MessageSource.SourceNameId),
                CreatorName = command.MessageSource.SourceName,
                TenantName = command.MessageSource.SourceNameId,
                Message = command.CommandDesc,
                MessageContent = JsonConvert.SerializeObject(command)
            });
                
        }
    }


    Guid ConvertoGuid(string guid)
    {
        var newGuid = Guid.Empty;
        Guid.TryParse(guid, out newGuid);
        return newGuid;
    }
}