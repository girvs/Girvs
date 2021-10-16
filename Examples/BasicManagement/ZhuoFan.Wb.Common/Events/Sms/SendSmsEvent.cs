using Girvs.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZhuoFan.Wb.Common.Events.Sms
{
    public class SendSmsEvent : IntegrationEvent
    {
        public SendSmsEvent()
        {

        }
        public SendSmsEvent(string contactNumber,
                                int smsMessageType,
                                string messageContent,
                                string examName,
                                Guid? tenantId)
        {
            ContactNumber = contactNumber;
            SmsMessageType = smsMessageType;
            MessageContent = messageContent;
            ExameName = examName;
            TenantId = tenantId;
        }

        /// <summary>
        /// 手机号
        /// </summary>
        public string ContactNumber { get;  set; }

        /// <summary>
        /// 短信类型（0:登录验证码 1:注册验证码 2:找回密码验证码 3:通知）
        /// </summary>
        public int SmsMessageType { get;  set; }

        /// <summary>
        /// 发送内容
        /// </summary>
        public string MessageContent { get;  set; }

        /// <summary>
        /// 考试名称
        /// </summary>
        public string ExameName { get;  set; }

        /// <summary>
        /// 租户id
        /// </summary>
        public Guid? TenantId { get;  set; }
    }
}
