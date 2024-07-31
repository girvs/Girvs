namespace Girvs.DynamicWebApi;

public interface IAppWebMiniApiService
{
    RouteGroupBuilder MapServiceMiniApi(IEndpointRouteBuilder app);
}
