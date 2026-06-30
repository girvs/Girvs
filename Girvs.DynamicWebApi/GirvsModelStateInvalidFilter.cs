namespace Girvs.DynamicWebApi;

public class GirvsModelStateInvalidFilter(
    IOptions<ApiBehaviorOptions> options,
    ILogger<GirvsModelStateInvalidFilter> logger
) : IActionFilter
{
    private ModelStateInvalidFilter _modelStateInvalidFilter = new(options.Value, logger);

    public void OnActionExecuting(ActionExecutingContext context)
    {
        _modelStateInvalidFilter.OnActionExecuting(context);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _modelStateInvalidFilter.OnActionExecuted(context);
    }
}
