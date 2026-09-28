namespace Girvs.EventBus.Identity;

internal static class IntegrationEventPrincipalFactory
{
    internal static ClaimsPrincipal Create(IDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (!headers.TryGetValue(IntegrationIdentityContextSerializer.HeaderName, out var json)
            || string.IsNullOrEmpty(json))
        {
            return new ClaimsPrincipal();
        }

        var context = IntegrationIdentityContextSerializer.Deserialize(json);
        var claims = context.Claims
            .Where(x => x.Type != GirvsClaimTypes.ExecutionSource)
            .Select(x => new Claim(
                x.Type,
                x.Value,
                string.IsNullOrEmpty(x.ValueType) ? ClaimValueTypes.String : x.ValueType,
                string.IsNullOrEmpty(x.Issuer) ? ClaimsIdentity.DefaultIssuer : x.Issuer))
            .ToList();
        claims.Add(new Claim(
            GirvsClaimTypes.ExecutionSource,
            ExecutionSource.EventBus.ToString()));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Girvs.EventBus"));
    }
}
