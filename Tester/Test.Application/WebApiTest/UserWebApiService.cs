using System;
using System.Threading.Tasks;
using Girvs.Domain;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Notifications;
using MediatR;
using Panda.DynamicWebApi;
using Panda.DynamicWebApi.Attributes;
using Test.Domain.Commands.User;
using Test.Domain.Models;

namespace Test.Application.WebApiTest
{
    [DynamicWebApi]
    public class UserWebApiService : IUserWebApiService, IDynamicWebApi
    {
        private readonly IMediatorHandler _bus;
        private readonly DomainNotificationHandler _notifications;

        public UserWebApiService(
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications
        )
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _notifications = notifications as DomainNotificationHandler ??
                             throw new ArgumentNullException(nameof(notifications));
        }

        public Task<dynamic> GetById(Guid id)
        {
            throw new Exception();
        }

        public async Task<Guid> CreateUser(User user)
        {
            CreateUserCommand command = new CreateUserCommand(user.UserAccount, user.UserPassword, user.UserName,
                user.ContactNumber, user.State, user.UserType);
            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                throw new GirvsException(errorMessage, errorCode);
            }

            return command.Id;
        }
    }
}