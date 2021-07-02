using System.Threading.Tasks;
using BasicManagement.Application.ViewModels.SysDict;
using Girvs.DynamicWebApi;

namespace BasicManagement.Application.AppService
{
    public interface ISysDictService : IAppWebApiService
    {
        Task<SysDictEditViewModel> CreateAsync(SysDictEditViewModel model);
        Task<SysDictQueryViewModel> GetAsync(SysDictQueryViewModel model);
    }
}
