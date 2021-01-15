using System;
using System.Threading.Tasks;
using Girvs.Application;
using Test.Domain.Models;

namespace Test.Application.WebApiTest
{
    public interface IUserWebApiService:IAppWebApiService
    {
        Task<dynamic> GetById(Guid id);
        Task<Guid> CreateUser(User user);
    }
}