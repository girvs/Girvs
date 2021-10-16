using System.ComponentModel.DataAnnotations;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.User
{
    /// <summary>
    /// 用户修改登陆密码模型
    /// </summary>
    public class UserEditPasswordViewModel
    {
        /// <summary>
        /// 新的用户密码
        /// </summary>
        [Required]
        public string NewPassword { get;  set; }
        
        /// <summary>
        /// 旧的用户密码
        /// </summary>
        [Required]
        public string OldPassword { get;  set; }
    }
}