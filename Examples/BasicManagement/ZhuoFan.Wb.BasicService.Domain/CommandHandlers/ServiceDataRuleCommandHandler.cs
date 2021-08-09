using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Girvs.BusinessBasis.UoW;
using Girvs.Cache.Caching;
using Girvs.Driven.Bus;
using Girvs.Driven.CacheDriven.Events;
using Girvs.Driven.Commands;
using Girvs.Extensions;
using JetBrains.Annotations;
using MediatR;
using ZhuoFan.Wb.BasicService.Domain.Commands.ServiceDataRule;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Domain.CommandHandlers
{
    public class ServiceDataRuleCommandHandler : CommandHandler, IRequestHandler<CreateOrUpdateServiceDataRuleCommand, bool>
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

        public async Task<bool> Handle(CreateOrUpdateServiceDataRuleCommand request, CancellationToken cancellationToken)
        {
            Expression<Func<ServiceDataRule, bool>> expression = x => x.ServiceName == request.ServiceName;
            expression = expression.And(x => x.UserType == request.UserType);
            expression = expression.And(x => x.ModuleName == request.ModuleName);
            expression = expression.And(x => x.FieldName == request.FieldName);

            var rule = await _repository.GetEntityByWhere(expression);


            if (rule != null)
            {
                rule.DataSource = request.DataSource;
                rule.FieldDesc = request.FieldDesc;
                rule.FieldName = request.FieldName;
                rule.ModuleName = request.ModuleName;
                rule.ServiceName = request.ServiceName;
                rule.UserType = request.UserType;

                await _repository.UpdateAsync(rule);
            }
            else
            {
                rule = new ServiceDataRule
                {
                    DataSource = request.DataSource,
                    FieldDesc = request.FieldDesc,
                    FieldName = request.FieldName,
                    ModuleName = request.ModuleName,
                    ServiceName = request.ServiceName,
                    UserType = request.UserType
                };

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