using System.Threading.Tasks;
using Girvs.Domain.IRepositories;
using SmartProducts.Person.Domain.Entities;

namespace SmartProducts.Person.Domain.Repositories
{
    public interface IPersonInfoRepository : IBaseActionRepository<PersonInfoEntity>
    {
        Task<PersonInfoEntity> GetPersonInfoByCardAsync(string card);
    }
}
