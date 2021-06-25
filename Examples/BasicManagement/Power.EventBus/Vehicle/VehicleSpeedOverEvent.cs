using Power.EventBus.Device;
using System;
using System.Collections.Generic;
using System.Text;

namespace Power.EventBus.Vehicle
{
    public class VehicleSpeedOverEvent : DeviceEventWithData<VehicleSpeedOverData>
    {

    }

    public class VehicleSpeedOverData
    {
        public int VehicleType { get; set; }
        public int Speed { get; set; }
        public string License { get; set; }

        public string ImageFile { get; set; }

        public int Limit { get; set; }

        public DateTime TakeTime { get; set; }
    }
}
