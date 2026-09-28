namespace Girvs.EventBus.Identity;

public sealed record IntegrationClaim(
    string Type,
    string Value,
    string ValueType,
    string Issuer);

public sealed record IntegrationIdentityContext(
    int Version,
    IReadOnlyCollection<IntegrationClaim> Claims);
