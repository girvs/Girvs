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
                MessageSource = new MessageSource();
                MessageSource.SourceName = EngineContext.Current.ClaimManager.IdentityClaim.UserName;
                MessageSource.SourceNameId = EngineContext.Current.ClaimManager.IdentityClaim.TenantId;
                MessageSource.TenantId = EngineContext.Current.ClaimManager.IdentityClaim.TenantId;
                MessageSource.TenantName = EngineContext.Current.ClaimManager.IdentityClaim.TenantName;
                if (EngineContext.Current != null && EngineContext.Current.HttpContext != null)
                {
                    MessageSource.IpAddress =
                        EngineContext.Current.HttpContext?.Request.Headers["X-Forwarded-For"].ToString();
                }
                else
                {
                    MessageSource.IpAddress = "localhost";
                }
            }
            catch
            {
                MessageSource.IpAddress = "localhost";
                // ignored
            }
        }
    }
}