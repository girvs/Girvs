using Girvs.Application;
using Girvs.Application.Mapper;

namespace Power.BasicManagement.Application.ViewModels.SysDict
{
    /// <summary>
    /// 
    /// </summary>
    [AutoMapTo(typeof(Domain.Models.SysDict))]
    [AutoMapFrom(typeof(Domain.Models.SysDict))]
    public class SysDictEditViewModel : IDto
    {
        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 字典类型
        /// </summary>
        public string CodeType { get; set; }
        ///<summary>编号</summary>
        public string Code { get; set; }

        ///<summary>名称</summary>
        public string Name { get; set; }

        ///<summary>描述</summary>
        public string Desc { get; set; }
    }
}
