using System.ComponentModel.DataAnnotations;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.User
{
    public class UserLoginViewModel
    {
        /// <summary>
        /// 用户名称
        /// </summary>
        [Required]
        public string UserAccount { get; set; }
        
        /// <summary>
        /// 用户登陆密码
        /// </summary>
        [Required]
        public string Password { get; set; }
    }
}