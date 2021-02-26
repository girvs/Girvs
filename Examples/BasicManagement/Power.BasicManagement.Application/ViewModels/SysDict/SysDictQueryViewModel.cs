using Girvs.Application;
using Girvs.Application.Mapper;
using Power.BasicManagement.Domain.Queries;

namespace Power.BasicManagement.Application.ViewModels.SysDict
{
    /// <summary>
    /// 
    /// </summary>
    [AutoMapFrom(typeof(SysDictQuery))]
    [AutoMapTo(typeof(SysDictQuery))]
    public class SysDictQueryViewModel : QueryDtoBase<SysDictEditViewModel>
    {
        /// <summary>
        /// 字典类型
        /// </summary>
        public string CodeType { get; set; }
    }
}
