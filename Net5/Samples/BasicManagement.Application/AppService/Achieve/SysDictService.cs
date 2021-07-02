using System.Threading.Tasks;
using BasicManagement.Application.ViewModels.SysDict;
using BasicManagement.Domain.Commands.SysDict;
using BasicManagement.Domain.Models;
using BasicManagement.Domain.Queries;
using BasicManagement.Domain.Repositories;
using Girvs;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.AutoMapper.Extensions;
using Girvs.Cache;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;

namespace BasicManagement.Application.AppService.Achieve
{
    /// <summary>
    /// 字典管理API
    /// </summary>
    [DynamicWebApi]
    [Authorize]
    [ServicePermissionDescriptor("字典管理", "96b8cb41-6051-11eb-b279-0894ef99499f")]
    [Microsoft.AspNetCore.Components.Route("api/dict")]
    public class SysDictService : ISysDictService
    {
        private readonly ISysDictRepository _sysDictRepository;
        private readonly IStaticCacheManager _cacheManager;
        private readonly IMediatorHandler _bus;
        private readonly DomainNotificationHandler _notifications;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sysDictRepository"></param>
        /// <param name="cacheManager"></param>
        /// <param name="keyManager"></param>
        /// <param name="bus"></param>
        /// <param name="notifications"></param>
        public SysDictService(
            ISysDictRepository sysDictRepository,
            IStaticCacheManager cacheManager,
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications
        )
        {
            this._sysDictRepository = sysDictRepository;
            this._cacheManager = cacheManager;
            this._bus = bus;
            this._notifications = (DomainNotificationHandler) notifications;
        }

        /// <summary>
        /// 创建字典
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceMethodPermissionDescriptor("新增", Permission.Post)]
        public async Task<SysDictEditViewModel> CreateAsync(SysDictEditViewModel model)
        {
            CreateSysDictCommand command = new CreateSysDictCommand(model.Name, model.Desc, model.Code, model.CodeType);
            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                throw new GirvsException(errorMessage, errorCode);
            }

            model.Id = command.Id;
            return model;
        }

        /// <summary>
        /// 修改字典
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        [ServiceMethodPermissionDescriptor("修改", Permission.Edit)]
        public async Task<SysDictEditViewModel> UpdateAsync(SysDictEditViewModel model)
        {
            UpdateSysDictCommand command =
                new UpdateSysDictCommand(model.Id, model.Name, model.Desc, model.Code, model.CodeType);
            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                throw new GirvsException(errorMessage, errorCode);
            }

            return model;
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ServiceMethodPermissionDescriptor("删除", Permission.Delete)]
        public async Task DeleteAsync(int id)
        {
            DeleteSysDictCommand command = new DeleteSysDictCommand(id);
            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                throw new GirvsException(errorMessage, errorCode);
            }
        }

        /// <summary>
        /// 分页查询字典
        /// </summary>
        /// <param name="queryModel"></param>
        /// <returns></returns>
        [HttpGet]
        [ServiceMethodPermissionDescriptor("浏览", Permission.View)]
        public async Task<SysDictQueryViewModel> GetAsync(SysDictQueryViewModel queryModel)
        {
            var query = queryModel.MapToQuery<SysDictQuery>();

            var tempQuery = await _cacheManager.GetAsync(
                GirvsEntityCacheDefaults<SysDict>.QueryCacheKey.Create(query.GetCacheKey()) ,async () =>
                {
                    await _sysDictRepository.GetByQueryAsync(query);
                    return query;
                });

            if (!query.Equals(tempQuery))
            {
                query.RecordCount = tempQuery.RecordCount;
                query.Result = tempQuery.Result;
            }

            return query.MapToQueryDto<SysDictQueryViewModel, SysDict>();
        }
    }
}