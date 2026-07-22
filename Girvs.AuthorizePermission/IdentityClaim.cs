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
                .DistinctBy(x => x.Type)
                .ToDictionary(x => x.Type, v => v.Value);
            var identityType = claims.GetDictionaryValueByKey(GirvsClaimTypes.IdentityType)
                .ToEnum<IdentityType>();

            if (identityType == IdentityType.RegisterUser)
            {
                var tenantId = httpContext.Request.Headers[nameof(GirvsIdentityClaim.TenantId)];
                var tenantName = httpContext.Request.Headers[nameof(GirvsIdentityClaim.TenantName)];

                claims.SetDictionaryKeyValue(GirvsClaimTypes.TenantId, tenantId);
                claims.SetDictionaryKeyValue(GirvsClaimTypes.TenantName, HttpUtility.UrlDecode(tenantName));
            }

            SetFromDictionary(claims);
        }
        else //处理前端没有登陆的情况
        {
            var claims = new Dictionary<string, string>();
            var requestHeaders = httpContext?.Request.Headers ?? new HeaderDictionary();
            if (requestHeaders.TryGetValue(nameof(GirvsIdentityClaim.TenantId), out var tenantId))
            {
                var tenantName = requestHeaders[nameof(GirvsIdentityClaim.TenantName)];
                claims.SetDictionaryKeyValue(GirvsClaimTypes.TenantId, tenantId);
                claims.SetDictionaryKeyValue(GirvsClaimTypes.TenantName, HttpUtility.UrlDecode(tenantName));
                claims.SetDictionaryKeyValue(GirvsClaimTypes.IdentityType,
                    IdentityType.RegisterUser.ToString());
            }

            if (claims.Any())
            {
                SetFromDictionary(claims);
            }
        }
    }

    public void SetFromDictionary(Dictionary<string, string> dictionary)
    {
        IdentityClaim = new GirvsIdentityClaim
        {
            OtherClaims = dictionary
        };

        IdentityClaim.UserId =
            dictionary.GetDictionaryValueByKey(GirvsClaimTypes.UserId);

        IdentityClaim.UserName =
            dictionary.GetDictionaryValueByKey(GirvsClaimTypes.UserName);

        IdentityClaim.TenantId =
            dictionary.GetDictionaryValueByKey(GirvsClaimTypes.TenantId);

        IdentityClaim.TenantName =
            dictionary.GetDictionaryValueByKey(GirvsClaimTypes.TenantName);

        var identityType = dictionary.GetDictionaryValueByKey(GirvsClaimTypes.IdentityType);
        if (!identityType.IsNullOrEmpty())
        {
            IdentityClaim.IdentityType =
                identityType.ToEnum<IdentityType>();
        }

        var systemModule = dictionary.GetDictionaryValueByKey(GirvsClaimTypes.SystemModule);

        if (!systemModule.IsNullOrEmpty())
        {
            IdentityClaim.SystemModule = systemModule.ToEnum<SystemModule>();
        }
    }

    public ClaimsIdentity BuildClaimsIdentity(GirvsIdentityClaim girvsIdentityClaim)
    {
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsClaimTypes.UserId,
            girvsIdentityClaim.UserId);
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsClaimTypes.UserName,
            girvsIdentityClaim.UserName);
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsClaimTypes.TenantId,
            girvsIdentityClaim.TenantId);
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsClaimTypes.TenantName,
            girvsIdentityClaim.TenantName);
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsClaimTypes.IdentityType,
            girvsIdentityClaim.IdentityType.ToString());
        girvsIdentityClaim.OtherClaims.SetDictionaryKeyValue(GirvsClaimTypes.SystemModule,
            girvsIdentityClaim.SystemModule.ToString());

        var claims = girvsIdentityClaim.OtherClaims.Select(x => new Claim(
            x.Key, x.Value));

        return new ClaimsIdentity(claims);
    }
}
