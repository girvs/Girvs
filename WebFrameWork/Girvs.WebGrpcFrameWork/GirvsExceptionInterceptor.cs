using System;
using System.Threading.Tasks;
using Girvs.Domain;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Girvs.WebGrpcFrameWork
{
    public class GirvsExceptionInterceptor : Interceptor
    {
        private readonly ILogger<GirvsExceptionInterceptor> _logger;

        public GirvsExceptionInterceptor(ILogger<GirvsExceptionInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            LogCall(context);
            try
            {
                return await continuation(request, context);
            }
            catch (GirvsException e)
            {
                _logger.LogError(e, e.Message);
                StatusCode status = ConverGrpcStatusCode(e.StatusCode);
                throw new RpcException(new Status(status, e.Message, e));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new RpcException(Status.DefaultCancelled, e.Message);
            }
        }

        private void LogCall(ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            _logger.LogDebug($"Starting call. Request: {httpContext.Request.Path}");
        }

        /// <summary>
        /// 需要将系统的StatusCodes转换成Grpc对应的错误代码
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        private StatusCode ConverGrpcStatusCode(int status)
        {
            return StatusCode.Cancelled;
        }

    }
}