using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace Test.GrpcServiceWebHost.Services
{
    [AllowAnonymous]
    public class AccountService : AccountGrpcService.AccountGrpcServiceBase
    {
        public override Task<ResponseToken> Login(RequestUserLogin request, ServerCallContext context)
        {
            var result = new ResponseToken()
            {
                Token = "testtest"
            };
            return Task.FromResult(result);
        }
    }
}