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
using ZhuoFan.Wb.BasicService.Domain.Commands.ServiceDataRule;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Domain.CommandHandlers
{
    public class ServiceDataRuleCommandHandler : CommandHandler,
        IRequestHandler<CreateOrUpdateServiceDataRuleCommand, bool>
    {
        private readonly IMediatorHandler _bus;
        private readonly IServiceDataRuleRepository _repository;

        public ServiceDataRuleCommandHandler(
            IUnitOfWork<ServiceDataRule> uow,
            [NotNull] IMediatorHandler bus,
            [NotNull] IServiceDataRuleRepository repository
        ) : base(uow, bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(CreateOrUpdateServiceDataRuleCommand request,
            CancellationToken cancellationToken)
        {
            Expression<Func<ServiceDataRule, bool>> expression = x =>
                x.EntityTypeName == request.EntityTypeName && x.FieldName == request.FieldName &&
                x.FieldType == request.FieldType && x.ExpressionType == request.ExpressionType;

            var rule = await _repository.GetEntityByWhere(expression);

            var exist = rule != null;

            if (!exist)
            {
                rule = new ServiceDataRule();
            }


            rule.EntityTypeName = request.EntityTypeName;
            rule.EntityDesc = request.EntityDesc;
            rule.FieldDesc = request.FieldDesc;
            rule.FieldName = request.FieldName;
            rule.FieldType = request.FieldType;
            rule.ExpressionType = request.ExpressionType;
            rule.FieldValue = request.FieldValue;
            rule.UserType = request.UserType;

            if (exist)
            {
                await _repository.UpdateAsync(rule);
            }
            else
            {
                await _repository.AddAsync(rule);
            }


            if (await Commit())
            {
                await _bus.RaiseEvent(
                    new RemoveCacheListEvent(GirvsEntityCacheDefaults<ServiceDataRule>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }
    }
}