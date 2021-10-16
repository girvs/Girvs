using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Girvs.BusinessBasis.UoW;
using Girvs.Cache.Caching;
using Girvs.Driven.Bus;
using Girvs.Driven.CacheDriven.Events;
using Girvs.Driven.Commands;
using JetBrains.Annotations;
using MediatR;
using ZhuoFan.Wb.BasicService.Domain.Commands.ServicePermission;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Domain.CommandHandlers
{
    public class ServicePermissionCommandHanlder : CommandHandler,
        IRequestHandler<CreateOrUpdateServicePermissionCommand, bool>
    {
        private readonly IMediatorHandler _bus;
        private readonly IServicePermissionRepository _servicePermissionRepository;

        public ServicePermissionCommandHanlder(
            [NotNull] IMediatorHandler bus,
            [NotNull] IUnitOfWork<ServicePermission> unitOfWork,
            [NotNull] IServicePermissionRepository servicePermissionRepository
        ) : base(unitOfWork, bus)
        {
            if (unitOfWork == null) throw new ArgumentNullException(nameof(unitOfWork));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _servicePermissionRepository = servicePermissionRepository ??
                                           throw new ArgumentNullException(nameof(servicePermissionRepository));
        }

        public async Task<bool> Handle(CreateOrUpdateServicePermissionCommand request,
            CancellationToken cancellationToken)
        {
            Expression<Func<ServicePermission, bool>> expression = x => x.ServiceId == request.ServiceId;

            var sp = await _servicePermissionRepository.GetEntityByWhere(expression);

            if (sp == null)
            {
                sp = new ServicePermission
                {
                    ServiceId = request.ServiceId,
                    ServiceName = request.ServiceName,
                    Permissions = request.Permissions,
                    OperationPermissions = request.OperationPermissionModels
                };
                await _servicePermissionRepository.AddAsync(sp);
            }
            else
            {
                sp.ServiceId = request.ServiceId;
                sp.ServiceName = request.ServiceName;
                sp.Permissions = request.Permissions;
                await _servicePermissionRepository.UpdateAsync(sp);
            }

            if (await Commit())
            {
                await _bus.RaiseEvent(
                    new RemoveCacheListEvent(GirvsEntityCacheDefaults<ServicePermission>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }
    }
}