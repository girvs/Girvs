namespace Girvs.DynamicWebApi;

public class GirvsModelStateInvalidFilter : IActionFilter
{
    private ModelStateInvalidFilter _modelStateInvalidFilter;

    public GirvsModelStateInvalidFilter(IOptions<ApiBehaviorOptions> options,
        ILogger<GirvsModelStateInvalidFilter> logger)
    {
        _modelStateInvalidFilter =
            new ModelStateInvalidFilter(options.Value, logger);
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        _modelStateInvalidFilter.OnActionExecuting(context);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _modelStateInvalidFilter.OnActionExecuted(context);
    }
}