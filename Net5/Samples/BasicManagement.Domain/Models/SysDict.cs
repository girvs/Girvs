using Girvs.BusinessBasis.Entities;

namespace BasicManagement.Domain.Models
{
    /// <summary>
    /// 系统字典
    /// </summary>
    public class SysDict : AggregateRoot<int>, IIncludeInitField
    {
        /// <summary>
        /// 字典分类
        /// </summary>
        public string CodeType { get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Desc { get; set; }
        /// <summary>
        /// 是否初始化数据
        /// </summary>
        public bool IsInitData { get; set; }
    }
}
