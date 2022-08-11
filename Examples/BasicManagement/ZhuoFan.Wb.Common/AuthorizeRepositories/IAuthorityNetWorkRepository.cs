namespace ZhuoFan.Wb.Common.AuthorizeRepositories;

[RefitService("ZhuoFan_Wb_BasicService_WebApi")]
public interface IAuthorityNetWorkRepository : IGirvsRefit
{
    [Get("/api/User/CurrentUserAuthorization")]
    Task<AuthorizeModel> GetUserAuthorization();
}