using System;
using System.Threading.Tasks;
using Girvs.Domain.Driven.Bus;
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

        public UserWebApiService(
            IMediatorHandler bus
        )
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public Task GetById(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<Guid> CreateUser(User user)
        {
            CreateUserCommand command = new CreateUserCommand(user.UserAccount, user.UserPassword, user.UserName,
                user.ContactNumber, user.State, user.UserType);
            await _bus.SendCommand(command);

            return command.Id;
        }
    }
}