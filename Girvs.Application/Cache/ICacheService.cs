using System.Collections.Generic;
using System.Threading.Tasks;

namespace Girvs.Application.Cache
{
    public interface ICacheService : IService
    {
        Task<List<string>> GetKeys();
        Task<string> GetValueByKey(string key);

        Task RemoveByPrefix(string prefix);

        Task RemoveByKey(string key);
    }
}