using System;
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
                MessageSource = new MessageSource
                {
                    SourceName = EngineContext.Current.ClaimManager.GetUserName(),
                    SourceNameId = EngineContext.Current.ClaimManager.GetUserId(),
                    TenantId = EngineContext.Current.ClaimManager.GetTenantId(),
                    TenantName = EngineContext.Current.ClaimManager.GetTenantName(),
                    IpAddress = EngineContext.Current.HttpContext.Request.Headers["X-Forwarded-For"].ToString()
                };
            }
            finally
            {
            }
        }
    }
}