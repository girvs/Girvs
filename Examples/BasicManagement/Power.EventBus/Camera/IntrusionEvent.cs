using Power.EventBus.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Power.EventBus.EventBus
{
    public class IntrusionEvent : DeviceEventWithData<IntrusionData>
    {

    }

    public class IntrusionData
    {
        public DateTime EnterTime { get; set; }
    }
}
