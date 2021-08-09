using System;
using Girvs.BusinessBasis.Entities;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Domain.Models
{
    public class UserRules: AggregateRoot<Guid>
    {
        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName { get; set; }
        
        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; set; }
        
        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 字段值
        /// </summary>
        public string FieldValue { get; set; }

        /// <summary>
        /// 对应操作
        /// </summary>
        public string Operate { get; set; }
    }
}