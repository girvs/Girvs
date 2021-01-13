using System;
using System.Threading.Tasks;
using Test.Domain.Models;

namespace Test.Application.WebApiTest
{
    public interface IUserWebApiService
    {
        Task GetById(Guid id);
        Task<Guid> CreateUser(User user);
    }
}