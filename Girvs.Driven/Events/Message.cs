using System;
using Girvs.Extensions;
using Girvs.Infrastructure;
using MediatR;

namespace Girvs.Driven.Events
{
    public abstract class Message : IRequest<bool>
    {
        public string MessageType { get; protected set; }
        public Guid AggregateId { get; protected set; }
        public MessageSource MessageSource { get; protected set; }

        protected Message()
        {
            MessageType = GetType().Name;
            try
            {
                MessageSource = new MessageSource();
                MessageSource.SourceName = EngineContext.Current.ClaimManager.GetTenantName();
                MessageSource.SourceNameId = EngineContext.Current.ClaimManager.GetUserId();
                MessageSource.TenantId = EngineContext.Current.ClaimManager.GetTenantId();
                MessageSource.TenantName = EngineContext.Current.ClaimManager.GetTenantName();
                MessageSource.IpAddress = EngineContext.Current.HttpContext?.Request.GetUserRemoteIpAddress();
            }
            finally
            {
            }
        }
    }
}