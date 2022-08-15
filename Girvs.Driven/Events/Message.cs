namespace Girvs.Driven.Events;

public record Message(string MessageType, Guid AggregateId, MessageSource MessageSource) : Message<bool>(MessageType,
    AggregateId, MessageSource)
{
    public Message() :this(string.Empty, Guid.Empty, null)
    {
    }
}

public record Message<TResponse>
    (string MessageType, Guid AggregateId, MessageSource MessageSource) : IRequest<TResponse>
{
    public Message() : this(string.Empty, Guid.Empty, null)
    {
        MessageType = GetType().Name;
        try
        {
            var identityClaim = EngineContext.Current.ClaimManager.IdentityClaim;
            var ipAddress = EngineContext.Current is {HttpContext: { }}
                ? EngineContext.Current.HttpContext?.Request.Headers["X-Forwarded-For"].ToString()
                : "localhost";

            MessageSource = new MessageSource(identityClaim.UserName, ipAddress, identityClaim.UserId,
                identityClaim.TenantId, identityClaim.TenantName);
        }
        catch
        {
            // ignored
        }
    }
}