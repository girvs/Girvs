using System;
using System.Threading.Tasks;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Managers;
using Panda.DynamicWebApi;
using Panda.DynamicWebApi.Attributes;
using Test.Domain.Commands.User;
using Test.Domain.Models;
using Test.Domain.Repositories;

namespace Test.Application.WebApiTest
{
    [DynamicWebApi]
    public class UserWebApiService : IUserWebApiService, IDynamicWebApi
    {
        private readonly IMediatorHandler _bus;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository2;
        private readonly IUnitOfWork _unitOfWork2;

        public UserWebApiService(
            IMediatorHandler bus,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IUserRepository userRepository2,
            IUnitOfWork unitOfWork2
        )
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _userRepository2 = userRepository2 ?? throw new ArgumentNullException(nameof(userRepository2));
            _unitOfWork2 = unitOfWork2 ?? throw new ArgumentNullException(nameof(unitOfWork2));
        }

        public async Task<dynamic> GetById(Guid id)
        {
            return await _userRepository.GetByIdAsync(id);
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