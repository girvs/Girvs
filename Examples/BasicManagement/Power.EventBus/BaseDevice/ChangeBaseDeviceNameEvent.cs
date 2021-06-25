using System;

namespace Power.EventBus.BaseDevice
{
    public class ChangeBaseDeviceNameEvent : IntegrationEvent
    {
        public ChangeBaseDeviceNameEvent()
        {
            
        }
        public ChangeBaseDeviceNameEvent(Guid baseDeviceId, string name)
        {
            BaseDeviceId = baseDeviceId;
            Name = name;
        }
        
        public Guid BaseDeviceId { get; set; }
        public string Name { get; set; }
    }
}