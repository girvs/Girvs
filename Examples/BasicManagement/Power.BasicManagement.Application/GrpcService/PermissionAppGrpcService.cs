using System;
using System.Threading.Tasks;
using Girvs.Application;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Power.BasicManagement.Application.AppService;
using Power.BasicManagement.Application.GrpcService.PermissionGrpcService;
using Power.BasicManagement.Domain.Extensions;

namespace Power.BasicManagement.Application.GrpcService
{
    [Authorize]
    public class PermissionAppGrpcService : PermissionGrpcService.PermissionGrpcService.PermissionGrpcServiceBase,IAppGrpcService
    {
        private readonly IPermissionAppService _permissionAppService;

        public PermissionAppGrpcService(
            IPermissionAppService permissionAppService
            )
        {
            _permissionAppService = permissionAppService ?? throw new ArgumentNullException(nameof(permissionAppService));
        }

        public override async Task<ResponseCurrentUserObjectPermission> GetCurrentUserAppliedObjectPermission(RequestCurrentUserObjectPermission request, ServerCallContext context)
        {
            var objectPermission= await _permissionAppService.Get(request.AppliedObjectID.ToGuid());
            var response = new ResponseCurrentUserObjectPermission();
            response.PermissionStr.Add(objectPermission);
            return response;
        }
    }
}