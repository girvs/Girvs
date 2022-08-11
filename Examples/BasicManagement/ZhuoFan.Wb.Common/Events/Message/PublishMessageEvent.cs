using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ZhuoFan.Wb.Common.Events.Message
{
    public record PublishMessageEvent: IntegrationEvent
    {
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 短信类型
        /// </summary>
        public MsgType MsgType { get; set; }

        /// <summary>
        /// 发送内容
        /// </summary>
        public string MessageContent { get; set; }

        /// <summary>
        /// 消息发布者
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// 消息接受者
        /// </summary>
        public string Receiver { get; set; }

        /// <summary>
        /// 用户标识（可以是手机号或者身份证）
        /// </summary>
        public string UserId { get; set; }
    }

    public enum MsgType
    {
        /// <summary>
        /// 通知（发所给所有人）
        /// </summary>
        [Description("通知")]
        Notice,
        /// <summary>
        /// 个人
        /// </summary>
        [Description("个人")]
        Personal
    }
}
