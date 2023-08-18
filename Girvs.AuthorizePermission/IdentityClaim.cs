namespace Girvs.AuthorizePermission;

/// <summary>
/// 身份标识描述实体类
/// </summary>
public class IdentityClaimManager : IGirvsClaimManager
{
    public string GetTenantId() => IdentityClaim.TenantId;

    public string GetUserId() => IdentityClaim.UserId;

    public string GetUserName() => IdentityClaim.UserName;

    public string GetTenantName() => IdentityClaim.TenantName;

    public IdentityType GetIdentityType() => IdentityClaim.IdentityType;

    public GirvsIdentityClaim IdentityClaim { get; set; }

    public void SetFromHttpRequestToken()
    {
        var httpContext = EngineContext.Current.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated == true)
        {
            //过滤掉重复的Bug
            var claims = httpContext.User.Claims
                .DistinctBy(x=>x.Type)
                .ToDictionary(x => x.Type, v => v.Value);
            var identityType = claims.GetDictionaryValueByKey(GirvsIdentityClaimTypes.IdentityType)
                .ToEnum<IdentityType>();

            if (identityType == IdentityType.RegisterUser)
            {
                var tenantId = httpContext.Request.Headers[nameof(GirvsIdentityClaim.TenantId)];
                var tenantName = httpContext.Request.Headers[nameof(GirvsIdentityClaim.TenantName)];

                claims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.TenantId, tenantId);
                claims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.TenantName, HttpUtility.UrlDecode(tenantName));
            }

            SetFromDictionary(claims);
        }
        else  //处理前端没有登陆的情况
        {
            var claims = new Dictionary<string, string>();
            var requestHeaders = httpContext?.Request.Headers ?? new HeaderDictionary();
            if (requestHeaders.TryGetValue(nameof(GirvsIdentityClaim.TenantId), out var tenantId))
            {
                var tenantName = requestHeaders[nameof(GirvsIdentityClaim.TenantName)];
                claims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.TenantId, tenantId);
                claims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.TenantName, HttpUtility.UrlDecode(tenantName));
            }
            
            SetFromDictionary(claims);
        }
    }

    public void SetFromDictionary(Dictionary<string, string> dictionary)
    {
        IdentityClaim = new GirvsIdentityClaim
        {
            OtherClaims = dictionary
        };

        IdentityClaim.UserId =
            dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.UserId);

        IdentityClaim.UserName =
            dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.UserName);

        IdentityClaim.TenantId =
            dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.TenantId);

        IdentityClaim.TenantName =
            dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.TenantName);

        var identityType = dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.IdentityType);
        if (!identityType.IsNullOrEmpty())
        {
            IdentityClaim.IdentityType =
                identityType.ToEnum<IdentityType>();
        }

        var systemModule = dictionary.GetDictionaryValueByKey(GirvsIdentityClaimTypes.ClaimSystemModule);

        if (!systemModule.IsNullOrEmpty())
        {
            IdentityClaim.SystemModule = systemModule.ToEnum<SystemModule>();
        }
    }

    public ClaimsIdentity BuildClaimsIdentity(GirvsIdentityClaim girvsIdentityClaim)
    {
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.UserId,
            girvsIdentityClaim.UserId);
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.UserName,
            girvsIdentityClaim.UserName);
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.TenantId,
            girvsIdentityClaim.TenantId);
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.TenantName,
            girvsIdentityClaim.TenantName);
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.IdentityType,
            girvsIdentityClaim.IdentityType.ToString());
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsIdentityClaimTypes.ClaimSystemModule,
            girvsIdentityClaim.SystemModule.ToString());

        var claims = girvsIdentityClaim.OtherClaims.Select(x => new Claim(
            x.Key, x.Value));

        return new ClaimsIdentity(claims);
    }
}