using System.Threading.Tasks;
using Girvs.Domain.IRepositories;
using Test.Domain.Models;

namespace Test.Domain.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetUserByLoginNameAsync(string loginName);
    }
}