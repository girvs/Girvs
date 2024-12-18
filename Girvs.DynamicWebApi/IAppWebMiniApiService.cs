namespace Girvs.DynamicWebApi;

#if NET9_0

public interface IAppWebMiniApiService
{
    RouteGroupBuilder MapServiceMiniApi(IEndpointRouteBuilder app);
}

#endif
