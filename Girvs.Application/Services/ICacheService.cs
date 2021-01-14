using System.Collections.Generic;
using System.Threading.Tasks;

namespace Girvs.Application.Services
{
    public interface ICacheService : IAppWebApiService
    {
        Task<List<string>> GetKeys();
        Task<string> GetValueByKey(string key);

        Task RemoveByPrefix(string prefix);

        Task RemoveByKey(string key);
    }
}