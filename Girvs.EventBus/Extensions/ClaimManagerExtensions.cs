namespace Girvs.EventBus.Extensions;

public static class ClaimManagerExtensions
{
    /// <summary>
    /// /重新设置请求头
    /// </summary>
    /// <param name="claimManager"></param>
    /// <param name="capHeader"></param>
    public static void CapEventBusReSetClaim(this IGirvsClaimManager claimManager, CapHeader capHeader)
    {
        var claimdic = capHeader.ToDictionary(x => x.Key, v => v.Value);
        claimdic.Remove(Headers.MessageId);
        claimdic.Remove(Headers.MessageName);
        claimdic.Remove(Headers.Group);
        claimdic.Remove(Headers.Type);
        claimdic.Remove(Headers.CorrelationId);
        claimdic.Remove(Headers.CorrelationSequence);
        claimdic.Remove(Headers.CallbackName);
        claimdic.Remove(Headers.SentTime);
        claimdic.Remove(Headers.Exception);
        claimdic.SetDictionaryKeyValue(GirvsIdentityClaimTypes.IdentityType,
            IdentityType.EventMessageUser.ToString());
        claimManager.SetFromDictionary(claimdic);
    }
}