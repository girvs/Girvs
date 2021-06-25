using Power.EventBus.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Power.EventBus.Acs
{
    /// <summary>
    /// 门禁进出事件
    /// </summary>
    public class AcsEntryEvent : DeviceEvent
    {
        /// <summary>
        /// 第三方设备Id
        /// </summary>
        public string ThirdPartyDeviceId { get; set; }
        /// <summary>
        /// 员工号
        /// </summary>
        public long EmployeeNo { get; set; }
        /// <summary>
        /// 卡号
        /// </summary>
        public string WorkCard { get; set; }
        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 进出时间
        /// </summary>
        public DateTime TakeTime { get; set; }
        /// <summary>
        /// 是否刷卡
        /// </summary>
        public bool ByCard { get; set; } = true;
        /// <summary>
        /// 方向，1为进，2为出
        /// </summary>
        public int Direction { get; set; }

        /// <summary>
        /// 人员Id
        /// </summary>
        public string PersonId { get; set; }
    }
}
