namespace Girvs.AuthorizePermission.Extensions;

public static class JwtBearerAuthenticationExtension
{
    public static string GenerateToken(
        string userId,
        string userName,
        string tenantId = null,
        string tenantName = null,
        UserType userType = UserType.All,
        IdentityType identityType = IdentityType.ManagerUser,
        SystemModule claimSystemModule = SystemModule.All)
    {
        var claims = new List<Claim>
        {
            new(GirvsClaimTypes.UserId, userId),
            new(GirvsClaimTypes.UserName, userName),
            new(GirvsClaimTypes.UserType, userType.ToString()),
            new(GirvsClaimTypes.IdentityType, identityType.ToString()),
            new(GirvsClaimTypes.SystemModule, claimSystemModule.ToString())
        };

        AddClaimIfNotEmpty(claims, GirvsClaimTypes.TenantId, tenantId);
        AddClaimIfNotEmpty(claims, GirvsClaimTypes.TenantName, tenantName);
        return GenerateToken(new ClaimsIdentity(claims));
    }

    public static string GenerateToken(ClaimsIdentity claimsIdentity)
    {
        ArgumentNullException.ThrowIfNull(claimsIdentity);
        return GetJwtAccessToken(claimsIdentity);
    }

    public static string GetJwtAccessToken(ClaimsIdentity claimsIdentity)
    {
        var authorizeConfig = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(authorizeConfig.JwtConfig.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claimsIdentity,
            Expires = DateTime.UtcNow.AddHours(authorizeConfig.JwtConfig.ExpiresHours),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static void AddClaimIfNotEmpty(List<Claim> claims, string type, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            claims.Add(new Claim(type, value));
        }
    }
}
