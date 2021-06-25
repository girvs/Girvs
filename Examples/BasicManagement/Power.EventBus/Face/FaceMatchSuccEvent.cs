using Power.EventBus.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Power.EventBus.Face
{
    public class FaceMatchSuccEvent : DeviceEventWithData<FaceMatchSuccData>
    {
    }

    public class FaceMatchSuccData : BaseEventData
    {
        public string ImageUrl { get; set; }

        public string IDNo { get; set; }
    }
}
