using System.ComponentModel.DataAnnotations;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.User
{
    
    /// <summary>
    /// 修改用户登陆密码模型
    /// </summary>
    public class ChangeUserPassworkViewModel
    {
        /// <summary>
        /// 用户新的登陆密码
        /// </summary>
        [Required]
        public string NewPassword { get; protected set; }
    }
}