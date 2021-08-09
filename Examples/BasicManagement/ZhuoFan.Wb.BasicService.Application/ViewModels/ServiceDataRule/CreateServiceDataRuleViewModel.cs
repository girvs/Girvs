using System.ComponentModel.DataAnnotations;
using Girvs.BusinessBasis.Dto;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.ServiceDataRule
{
    public class CreateServiceDataRuleViewModel : IDto
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        [Required]
        public string ServiceName { get; set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        [Required]
        public string ModuleName { get; set; }

        /// <summary>
        /// 用户类型
        /// </summary>
        [Required]
        public UserType UserType { get; set; }

        /// <summary>
        /// 字段对应的数据来源（即接口名称）
        /// </summary>
        [Required]
        public string DataSource { get; set; }

        /// <summary>
        /// 字段名称
        /// </summary>
        [Required]
        public string FieldName { get; set; }

        /// <summary>
        /// 字段的说明
        /// </summary>
        public string FieldDesc { get; set; }
    }
}