using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.User
{
    /// <summary>
    /// 启用禁用用户模型
    /// </summary>
    public class ChangeUserDataStateViewModel
    {
        /// <summary>
        /// 用户状态
        /// </summary>
        public DataState DataState { get; set; }
    }
}