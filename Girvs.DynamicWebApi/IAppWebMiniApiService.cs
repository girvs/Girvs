namespace Girvs.DynamicWebApi;

#if NET9_0_OR_GREATER

public interface IAppWebMiniApiService
{
    RouteGroupBuilder MapServiceMiniApi(IEndpointRouteBuilder app);
}

#endif
