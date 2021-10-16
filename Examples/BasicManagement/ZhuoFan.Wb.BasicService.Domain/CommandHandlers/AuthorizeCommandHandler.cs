using System;
using System.Collections.Generic;
using System.Linq;
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
using ZhuoFan.Wb.BasicService.Domain.Commands.Authorize;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Domain.CommandHandlers
{
    public class AuthorizeCommandHandler : CommandHandler,
        IRequestHandler<NeedAuthorizeListCommand, bool>
    {
        [NotNull] private readonly IMediatorHandler _bus;
        [NotNull] private readonly IUnitOfWork<ServicePermission> _unitOfWork;
        [NotNull] private readonly IServicePermissionRepository _servicePermissionRepository;
        private readonly IServiceDataRuleRepository _serviceDataRuleRepository;

        public AuthorizeCommandHandler(
            [NotNull] IMediatorHandler bus,
            [NotNull] IUnitOfWork<ServicePermission> unitOfWork,
            [NotNull] IServicePermissionRepository servicePermissionRepository,
            [NotNull] IServiceDataRuleRepository serviceDataRuleRepository
        ) : base(unitOfWork, bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _servicePermissionRepository = servicePermissionRepository ??
                                           throw new ArgumentNullException(nameof(servicePermissionRepository));
            _serviceDataRuleRepository = serviceDataRuleRepository ??
                                         throw new ArgumentNullException(nameof(serviceDataRuleRepository));
        }

        public async Task<bool> Handle(NeedAuthorizeListCommand request, CancellationToken cancellationToken)
        {
            Expression<Func<ServicePermission, bool>> permissionExpression = x =>
                request.ServicePermissionCommandModels.Select(x => x.ServiceId).ToList().Contains(x.ServiceId);

            var oldsps = await _servicePermissionRepository.GetWhereAsync(permissionExpression);

            var newsps = new List<ServicePermission>();
            
            newsps.AddRange(request.ServicePermissionCommandModels.Select(x=>new ServicePermission()
            {
                ServiceId = x.ServiceId,
                ServiceName = x.ServiceName,
                OperationPermissions = x.OperationPermissionModels,
                Permissions = x.Permissions
            }));

            await _servicePermissionRepository.DeleteRangeAsync(oldsps);
            await _servicePermissionRepository.AddRangeAsync(newsps);


            var entityTypeNames = request.ServiceDataRuleCommandModels.Select(x => x.EntityTypeName);
            var fieldNames = request.ServiceDataRuleCommandModels.Select(x => x.FieldName);

            var fieldTypes = request.ServiceDataRuleCommandModels.Select(x => x.FieldType);

            Expression<Func<ServiceDataRule, bool>> dataRuleExpression = x =>
                entityTypeNames.Contains(x.EntityTypeName) && fieldNames.Contains(x.FieldName) &&
                fieldTypes.Contains(x.FieldType);
                
            var oldRules = await _serviceDataRuleRepository.GetWhereAsync(dataRuleExpression);

            var newRules = new List<ServiceDataRule>();
            
            newRules.AddRange(request.ServiceDataRuleCommandModels.Select(x=>new ServiceDataRule()
            {
                EntityTypeName = x.EntityTypeName,
                EntityDesc = x.EntityDesc,
                FieldName = x.FieldName,
                FieldType = x.FieldType,
                FieldValue = x.FieldValue,
                FieldDesc = x.FieldDesc,
                ExpressionType = x.ExpressionType,
                UserType = x.UserType
            }));


            await _serviceDataRuleRepository.DeleteRangeAsync(oldRules);
            await _serviceDataRuleRepository.AddRangeAsync(newRules);
            

            if (await Commit())
            {
                await _bus.RaiseEvent(
                    new RemoveCacheListEvent(GirvsEntityCacheDefaults<ServiceDataRule>.ListCacheKey.Create()),
                    cancellationToken);
                await _bus.RaiseEvent(
                    new RemoveCacheListEvent(GirvsEntityCacheDefaults<ServicePermission>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }
    }
}