using Girvs.AuthorizePermission;
using Microsoft.AspNetCore.Authorization;
using Panda.DynamicWebApi.Attributes;

namespace ZhuoFan.Wb.BasicService.Application.AppService.Achieve
{
    [DynamicWebApi]
    [Authorize(AuthenticationSchemes = GirvsAuthenticationScheme.GirvsJwt)]
    public class PermissionAppService : IPermissionAppService
    {

    }
}