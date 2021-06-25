using Power.EventBus.Device;
using System;
using System.Collections.Generic;
using System.Text;

namespace Power.EventBus.Face
{
    public class FaceMatchFailEvent : DeviceEventWithData<FaceMatchFailData>
    {
    }

    public class FaceMatchFailData : BaseEventData
    {
        public string ImageUrl { get; set; }
    }
}
