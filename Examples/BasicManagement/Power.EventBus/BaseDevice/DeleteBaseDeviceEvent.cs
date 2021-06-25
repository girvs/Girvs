using System;

namespace Power.EventBus.BaseDevice
{
    public class DeleteBaseDeviceEvent : IntegrationEvent
    {
        public DeleteBaseDeviceEvent()
        {
        }

        public DeleteBaseDeviceEvent(Guid baseDeviceId)
        {
            BaseDeviceId = baseDeviceId;
        }

        public Guid BaseDeviceId { get; set; }
    }
}