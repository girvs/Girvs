using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Girvs.Grpc
{
    public class GirvsExceptionInterceptor : Interceptor
    {
        private readonly ILogger<GirvsExceptionInterceptor> _logger;

        public GirvsExceptionInterceptor(ILogger<GirvsExceptionInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            LogCall(context);
            try
            {
                return await continuation(request, context);
            }
            catch (Exception e)
            {
                Status status;

                if (e is GirvsException girvsException)
                {
                    status = new Status(ConverGrpcStatusCode(girvsException.StatusCode), e.Message);
                }
                else
                {
                    status = new Status(StatusCode.Unknown, e.Message);
                }

                _logger.LogError(e, e.Message);
                throw new RpcException(status);
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
            switch (status)
            {
                case StatusCodes.Status422UnprocessableEntity:
                    return StatusCode.Unavailable;
                case StatusCodes.Status404NotFound:
                    return StatusCode.NotFound;
            }

            return StatusCode.Cancelled;
        }
    }
}