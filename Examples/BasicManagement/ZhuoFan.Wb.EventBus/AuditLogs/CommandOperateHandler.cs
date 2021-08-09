using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Driven.Behaviors;
using Girvs.Driven.Commands;
using Girvs.EventBus;
using Girvs.Extensions;
using Girvs.Infrastructure;
using Newtonsoft.Json;

namespace AuditLogs
{
    public class CommandOperateHandler : ICommandOperateHandler
    {
        private readonly IEventBus _eventBus;

        public CommandOperateHandler(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task Handle(Command command)
        {
            await _eventBus.PublishAsync<CreateAuditLogEvent>(new CreateAuditLogEvent()
            {
                CancellationToken = default(CancellationToken),
                SourceType = SourceType.Server,
                AddressIp = EngineContext.Current.HttpContext.Request.GetUserRemoteIpAddress(),
                TenantId = ConvertoGuid(EngineContext.Current.ClaimManager.GetTenantId()),
                CreatorId = ConvertoGuid(EngineContext.Current.ClaimManager.GetUserId()),
                CreatorName = EngineContext.Current.ClaimManager.GetUserName(),
                TenantName = EngineContext.Current.ClaimManager.GetTenantName(),
                Message = command.CommandDesc,
                MessageContent = JsonConvert.SerializeObject(command)
            });
        }


        Guid ConvertoGuid(string guid)
        {
            var newGuid = Guid.Empty;
            Guid.TryParse(guid, out newGuid);
            return newGuid;
        }
    }
}