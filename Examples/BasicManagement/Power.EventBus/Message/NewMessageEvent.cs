using System;
using System.Collections.Generic;
using System.Text;

namespace Power.EventBus.Message
{
    public class NewMessageEvent : IntegrationEvent
    {
        public string DeviceId { get; set; }

        public string DeviceAddress { get; set; }

        public string PersonId { get; set; }

        public string EventType { get; set; }

        public dynamic AttachValue { get; set; }
    }
}
