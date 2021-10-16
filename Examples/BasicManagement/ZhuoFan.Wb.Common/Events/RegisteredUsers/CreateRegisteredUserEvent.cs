using Girvs.EventBus;

namespace ZhuoFan.Wb.Common.Events.RegisteredUsers
{
    /// <summary>
    /// 创建注册用户事件
    /// </summary>
    public class CreateRegisteredUserEvent : IntegrationEvent
    {
        public CreateRegisteredUserEvent()
        {

        }
        public CreateRegisteredUserEvent(string userName, string userPassword, string contactNumber, string cardId)
        {
            UserName = userName;
            UserPassword = userPassword;
            ContactNumber = contactNumber;
            CardId = cardId;
        }
        /// <summary>
        /// 姓名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string UserPassword { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string ContactNumber { get; set; }

        /// <summary>
        /// 身份证号
        /// </summary>
        public string CardId { get; set; }
    }
}
