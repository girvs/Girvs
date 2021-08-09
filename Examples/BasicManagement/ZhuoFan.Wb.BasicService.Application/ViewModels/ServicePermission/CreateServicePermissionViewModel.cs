using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Girvs.BusinessBasis.Dto;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.ServicePermission
{
    public class CreateServicePermissionViewModel : IDto
    {
        public CreateServicePermissionViewModel()
        {
            Permissions = new Dictionary<string, string>();
        }

        /// <summary>
        /// 功能模块名称，也可以是服务名称，更多以功能模块为主
        /// </summary>
        [Required]
        public string ServiceName { get; set; }
        
        /// <summary>
        /// 功能模块ID，必须唯一
        /// </summary>
        [Required]
        public Guid ServiceId { get; set; }
        
        /// <summary>
        /// 所需要授权的功能列表
        /// </summary>
        public Dictionary<string, string> Permissions { get; set; }
    }
}