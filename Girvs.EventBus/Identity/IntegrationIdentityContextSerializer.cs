using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Girvs.EventBus.Identity;

public static class IntegrationIdentityContextSerializer
{
    public const string HeaderName = "girvs-identity";
    public const int CurrentVersion = 1;
    public const int MaxHeaderBytes = 8192;

    private static readonly HashSet<string> AllowedClaimTypes =
    [
        GirvsClaimTypes.UserId,
        GirvsClaimTypes.UserName,
        GirvsClaimTypes.TenantId,
        GirvsClaimTypes.TenantName,
        GirvsClaimTypes.IdentityType,
        GirvsClaimTypes.SystemModule,
        GirvsClaimTypes.UserType,
        GirvsClaimTypes.ExecutionSource,
        GirvsClaimTypes.ClientId
    ];

    public static string Serialize(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var claims = principal.Claims
            .Where(x => AllowedClaimTypes.Contains(x.Type))
            .Select(x => new IntegrationClaim(x.Type, x.Value, x.ValueType, x.Issuer))
            .ToArray();
        if (claims.Length == 0)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(
            new IntegrationIdentityContext(CurrentVersion, claims));
        ValidateSize(json);
        return json;
    }

    public static IntegrationIdentityContext Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new GirvsException("EventBus 身份上下文不能为空");
        }

        ValidateSize(json);
        try
        {
            var context = JsonSerializer.Deserialize<IntegrationIdentityContext>(json);
            if (context == null || context.Version != CurrentVersion || context.Claims == null)
            {
                throw new GirvsException("EventBus 身份上下文版本不受支持");
            }

            if (context.Claims.Any(x =>
                    string.IsNullOrEmpty(x.Type) ||
                    x.Value == null ||
                    !AllowedClaimTypes.Contains(x.Type)))
            {
                throw new GirvsException("EventBus 身份上下文包含不允许的 Claim");
            }

            return context;
        }
        catch (GirvsException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new GirvsException("EventBus 身份上下文格式无效", exception);
        }
    }

    private static void ValidateSize(string json)
    {
        var byteCount = Encoding.UTF8.GetByteCount(json);
        if (byteCount > MaxHeaderBytes)
        {
            throw new GirvsException($"EventBus 身份上下文超过 {MaxHeaderBytes} 字节，实际 {byteCount} 字节");
        }
    }
}
