using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.Managers;
using Test.Domain.Models;

namespace Test.Domain.Managers
{
    public interface IStructureManager : IManager
    {
        Task<List<Structure>> GetAllListAsync();
    }
}