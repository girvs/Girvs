using Power.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Power.EventBus.Device
{
    public class DeviceEvent : IntegrationEvent
    {
        public string DeviceId { get; set; }
        public string DeviceAddress { get; set; }
        public string NvrDeviceId { get; set; }  
        public int NvrChannel { get; set; }
        public EventMessageType EventType { get; set; }
    }

    public class DeviceEventWithData<T> : DeviceEvent
    {
        public T EventData { get; set; }
    }

    public class BaseEventData
    {
        
    }
}
