using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicManagement.Domain.Commands.BasalPermission;
using BasicManagement.Domain.Models;
using BasicManagement.Domain.Repositories;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.UoW;
using Girvs.Driven.Bus;
using Girvs.Driven.Commands;
using MediatR;

namespace BasicManagement.Domain.CommandHandlers
{
    public class SaveBasalPermissionCommandHandler : CommandHandler, IRequestHandler<SaveBasalPermissionCommand, bool>
    {
        private readonly IMediatorHandler _bus;
        private readonly IPermissionRepository _permissionRepository;

        public SaveBasalPermissionCommandHandler(
            IMediatorHandler bus,
            IPermissionRepository permissionRepository,
            IUnitOfWork<BasalPermission> unitOfWork
            ) : base(unitOfWork, bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        }

        public async Task<bool> Handle(SaveBasalPermissionCommand request, CancellationToken cancellationToken)
        {
            var oldBasalPermissions = request.AppliedObjectType == PermissionAppliedObjectType.Role
                ? await _permissionRepository.GetRolePermissionLimit(request.AppliedID)
                : await _permissionRepository.GetUserPermissionLimit(request.AppliedID);

            await _permissionRepository.DeleteRangeAsync(oldBasalPermissions);

            var newBasalPermissions = request.BasalPermissionDtos.Select(x =>
            {
                var bp = new BasalPermission()
                {
                    AppliedID = request.AppliedID,
                    AppliedObjectID = x.AppliedObjectID,
                    AppliedObjectType = request.AppliedObjectType,
                    ValidateObjectID = x.ValidateObjectID,
                    ValidateObjectType = request.ValidateObjectType
                };
                foreach (var permission in x.Permissions)
                {
                    bp.SetBit(permission,AccessControlEntry.Allow);
                }
                return bp;
            }).ToList();

            await _permissionRepository.AddRangeAsync(newBasalPermissions);

            return await Commit();
        }
    }
}