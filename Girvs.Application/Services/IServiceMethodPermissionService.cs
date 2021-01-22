using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Application.Dtos;

namespace Girvs.Application.Services
{
    public interface IServiceMethodPermissionService : IAppWebApiService
    {
        Task<List<ServiceMethodPermissionListDto>> Get();
    }
}