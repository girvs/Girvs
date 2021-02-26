
using Girvs.Application;
using Power.BasicManagement.Application.ViewModels.SysDict;
using System.Threading.Tasks;

namespace Power.BasicManagement.Application.AppService
{
    public interface ISysDictService : IAppWebApiService
    {
        Task<SysDictEditViewModel> CreateAsync(SysDictEditViewModel model);
        Task<SysDictQueryViewModel> GetAsync(SysDictQueryViewModel model);
    }
}
