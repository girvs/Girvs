using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.UoW;
using Girvs.Driven.Bus;
using Girvs.Driven.Commands;
using MediatR;
using ZhuoFan.Wb.BasicService.Domain.Commands.BasalPermission;
using ZhuoFan.Wb.BasicService.Domain.Events;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Domain.CommandHandlers
{
    public class BasalPermissionCommandHandler : CommandHandler,
        IRequestHandler<SavePermissionCommand, bool>
    {
        private readonly IMediatorHandler _bus;
        private readonly IPermissionRepository _permissionRepository;

        public BasalPermissionCommandHandler(
            IMediatorHandler bus,
            IPermissionRepository permissionRepository,
            IUnitOfWork<BasalPermission> unitOfWork
        ) : base(unitOfWork, bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _permissionRepository =
                permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        }

        public async Task<bool> Handle(SavePermissionCommand request, CancellationToken cancellationToken)
        {
            var oldPermissions = await _permissionRepository.GetWhereAsync(x =>
                x.AppliedObjectType == request.AppliedObjectType &&
                request.ValidateObjectType == x.ValidateObjectType && request.AppliedID == x.AppliedID);

            await _permissionRepository.DeleteRangeAsync(oldPermissions);

            var newPermissions = request.ObjectPermissions.Select(x =>
                {
                    var bp = new BasalPermission()
                    {
                        AppliedID = request.AppliedID,
                        AppliedObjectID = x.AppliedObjectID,
                        AppliedObjectType = request.AppliedObjectType,
                        ValidateObjectType = request.ValidateObjectType
                    };
                    foreach (var permission in x.PermissionOpeation)
                    {
                        bp.SetBit(permission, AccessControlEntry.Allow);
                    }

                    return bp;
                }
            ).ToList();

            await _permissionRepository.AddRangeAsync(newPermissions);
            if (await Commit())
            {
                await _bus.RaiseEvent(new RemoveServiceCacheEvent(), cancellationToken);
            }

            return true;
        }
    }
}