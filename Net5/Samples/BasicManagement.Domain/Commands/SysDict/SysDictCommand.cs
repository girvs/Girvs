using Girvs.Driven.Commands;

namespace BasicManagement.Domain.Commands.SysDict
{
    public abstract class SysDictCommand : Command
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }
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
    }
}
