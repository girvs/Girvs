﻿using System.Threading;
using System.Threading.Tasks;
using BasicManagement.Domain.Commands.SysDict;
using BasicManagement.Domain.Models;
using BasicManagement.Domain.Repositories;
using Girvs.BusinessBasis.UoW;
using Girvs.Driven.Bus;
using Girvs.Driven.Commands;
using Girvs.Driven.Notifications;
using MediatR;

namespace BasicManagement.Domain.CommandHandlers
{
    /// <summary>
    /// 
    /// </summary>
    public class SysDictCommandHandler : CommandHandler,
        IRequestHandler<CreateSysDictCommand, bool>,
        IRequestHandler<UpdateSysDictCommand, bool>,
        IRequestHandler<DeleteSysDictCommand, bool>
    {
        private readonly ISysDictRepository _sysDictRepository;
        private readonly IMediatorHandler _bus;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uow"></param>
        /// <param name="bus"></param>
        /// <param name="sysDictRepository"></param>
        /// <param name="cacheKeyManager"></param>
        public SysDictCommandHandler(
            IUnitOfWork<SysDict> uow, 
            IMediatorHandler bus, 
            ISysDictRepository sysDictRepository
            ) : base(uow, bus)
        {
            _sysDictRepository = sysDictRepository;
            _bus = bus;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> Handle(CreateSysDictCommand request, CancellationToken cancellationToken)
        {
            var sysDict = new SysDict()
            {
                Name = request.Name,
                Desc = request.Desc,
                Code = request.Code,
                CodeType = request.CodeType
            };

            await _sysDictRepository.AddAsync(sysDict);

            if (await Commit())
            {
                request.Id = sysDict.Id;
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> Handle(UpdateSysDictCommand request, CancellationToken cancellationToken)
        {
            var sysDict = await _sysDictRepository.GetByIdAsync(request.Id);
            if (sysDict == null)
            {
                await _bus.RaiseEvent(new DomainNotification("", "未找到对应的字典"));
                return false;
            }

            sysDict.Name = request.Name;
            sysDict.Desc = request.Desc;
            sysDict.CodeType = request.CodeType;
            sysDict.Code = request.Code;

            await _sysDictRepository.UpdateAsync(sysDict);

            if (await Commit())
            {
                // await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(sysDict.Id)));
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }

        public async Task<bool> Handle(DeleteSysDictCommand request, CancellationToken cancellationToken)
        {
            var sysDict = await _sysDictRepository.GetByIdAsync(request.Id);
            if (sysDict == null)
            {
                await _bus.RaiseEvent(new DomainNotification("", "未找到对应的数据"));
                return false;
            }

            if (sysDict.IsInitData)
            {
                await _bus.RaiseEvent(new DomainNotification("", "系统初始化数据，无法进行操作"));
                return false;
            }

            await _sysDictRepository.DeleteAsync(sysDict);

            if (await Commit())
            {
                // await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(sysDict.Id)));
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }
    }
}
