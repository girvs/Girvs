using System.Diagnostics;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "开始处理 MediatR 请求：{RequestName}，请求参数：{@Request}",
            requestName,
            request);

        try
        {
            var response = await next();

            stopwatch.Stop();

            logger.LogInformation(
                "MediatR 请求处理完成：{RequestName}，耗时：{ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            logger.LogWarning(
                "MediatR 请求已取消：{RequestName}，耗时：{ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            logger.LogError(
                exception,
                "MediatR 请求处理异常：{RequestName}，耗时：{ElapsedMilliseconds}ms，请求参数：{@Request}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                request);

            throw;
        }
    }
}