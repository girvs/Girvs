using System;
using Girvs.EventBus;

namespace Articles
{
    /// <summary>
    /// 文书修改事件
    /// </summary>
    public class EditActicleEvent : IntegrationEvent
    {
        /// <summary>
        /// 文书Id
        /// </summary>
        public Guid ActicleId { get; set; }

        /// <summary>
        /// 文书名称
        /// </summary>
        public string ActicleName { get; set; }

        /// <summary>
        /// 文书内容
        /// </summary>
        public string ActicleContent { get; set; }
    }
}