using BasicManagement.Domain.Queries;
using Girvs.AutoMapper.Mapper;
using Girvs.BusinessBasis.Dto;

namespace BasicManagement.Application.ViewModels.SysDict
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
