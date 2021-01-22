using System.Collections.Generic;
using System.Threading.Tasks;

namespace Girvs.Application.Services
{
    public interface ICacheService : IAppWebApiService
    {
        Task<List<string>> GetKeys();
        Task<string> Get(string key);

        Task DeleteByPrefix(string prefix);

        Task DeleteByKey(string key);

        Task DeleteAll();
    }
}