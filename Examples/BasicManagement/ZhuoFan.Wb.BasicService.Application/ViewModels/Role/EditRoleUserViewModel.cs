using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Girvs.BusinessBasis.Dto;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.Role
{
    /// <summary>
    /// 角色用户操作模型
    /// </summary>
    public class EditRoleUserViewModel:IDto
    {
        public EditRoleUserViewModel()
        {
            UserIds = new List<Guid>();
        }
        
        /// <summary>
        /// 用户ID列表
        /// </summary>
        [Required]
        public IList<Guid> UserIds { get; set; }
    }
}