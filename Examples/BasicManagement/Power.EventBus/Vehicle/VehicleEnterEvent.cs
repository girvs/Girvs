using Power.EventBus.Device;
using System;
using System.Collections.Generic;
using System.Text;

namespace Power.EventBus.Vehicle
{
    public class VehicleEnterEvent : DeviceEventWithData<VehicleEnterData>
    {

    }

    public class VehicleEnterData
    {
        public string VehicleLaneKey { get; set; }
        public string Ipaddr { get; set; }
        public string License { get; set; }
        public string ColorType { get; set; }
        public string Type { get; set; }
        public string Confidence { get; set; }
        public string ScanTime { get; set; }
        public string ImageFile { get; set; }
        public string ImageFragmentFile { get; set; }
    }
}
