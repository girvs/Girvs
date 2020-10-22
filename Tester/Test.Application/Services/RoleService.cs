using System;
using System.Threading.Tasks;
using Girvs.Application;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.IRepositories;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Test.Domain.Commands.Role;
using Test.Domain.Extensions;
using Test.Domain.Models;
using Test.GrpcService.BaseServices.Public;
using Test.GrpcService.BaseServices.RoleGrpcService;

namespace Test.Application.Services
{
    [Authorize]
    public class RoleService : RoleGrpcService.RoleGrpcServiceBase, IAppGrpcService
    {
        private readonly IRepository<Role> _roleRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly ICacheKeyManager<Role> _cacheKeyManager;
        private readonly IMediatorHandler _bus;

        public RoleService(IRepository<Role> roleRepository,
            IStaticCacheManager staticCacheManager,
            ICacheKeyManager<Role> cacheKeyManager,
            IMediatorHandler bus)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public override async Task<GetByIdResponse> GetById(MainKeyMessage request, ServerCallContext context)
        {
            var role = await _staticCacheManager.GetAsync(
                _cacheKeyManager.BuildCacheEntityKey(request.Id.ToGuid()),
                async () => await _roleRepository.GetByIdAsync(request.Id.ToGuid()), 60);

            if (role == null)
                throw new RpcException(new Status(StatusCode.NotFound, "未找到对应的实体"));

            return new GetByIdResponse
            {
                RoleMessage = new RoleMessage
                {
                    Name = role.Name,
                    Desc = role.Desc,
                    Id = role.Id.ToString()
                }
            };
        }

        public override async Task<GetAllResponse> GetAll(Empty request, ServerCallContext context)
        {
            var list = await _staticCacheManager.GetAsync(_cacheKeyManager.CacheKeyListAllPrefix, async () =>
                await _roleRepository.GetAllAsync(), 60);


            var result = new GetAllResponse();

            list.ForEach(x => result.RoleMessages.Add(new RoleMessage
            {
                Name = x.Name,
                Desc = x.Desc,
                Id = x.Id.ToString()
            }));

            return result;
        }

        public override async Task<EditResponse> Add(EditRequest request, ServerCallContext context)
        {
            CreateRoleCommand command = new CreateRoleCommand(request.RoleMessage.Name, request.RoleMessage.Desc);
            await _bus.SendCommand(command);
            return new EditResponse
            {
                RoleMessage = new RoleMessage
                {
                    Id = command.Id.ToString(),
                    Name = command.Name,
                    Desc = command.Desc
                }
            };
        }

        public override async Task<EditResponse> Update(EditRequest request, ServerCallContext context)
        {
            UpdateRoleCommand command = new UpdateRoleCommand(request.RoleMessage.Name, request.RoleMessage.Desc);
            await _bus.SendCommand(command);
            return new EditResponse
            {
                RoleMessage = new RoleMessage
                {
                    Id = command.Id.ToString(),
                    Name = command.Name,
                    Desc = command.Desc
                }
            };
        }

        public override async Task<Empty> Delete(MainKeyMessage request, ServerCallContext context)
        {
            DeleteRoleCommand command = new DeleteRoleCommand(request.Id.ToGuid());
            await _bus.SendCommand(command);
            return new Empty();
        }
    }
}