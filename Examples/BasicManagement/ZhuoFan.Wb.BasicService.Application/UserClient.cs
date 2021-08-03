using System.Threading.Tasks;
using Girvs.Refit;
using Refit;

namespace ZhuoFan.Wb.BasicService.Application
{
    [RefitService("ZhuoFan_Wb_BasicService_WebApi")]
    public interface UserClient : IGirvsRefit
    {
        // [Get("/api/User/Token/{account}/{password}")]
        // [Headers("Authorization: Bearer")]
        // Task<string> GetToken([AliasAs("account")] string userName, [AliasAs("password")] string password);

        [Get("/api/User/58205e0e-1552-4282-bedc-a92d0afb37df")]
        // [Headers("Authorization: " + GirvsAuthenticationScheme.GirvsJwt)]
        Task<dynamic> GetUserById();
    }
}