using Power.EventBus.Device;
using System;
using System.Collections.Generic;
using System.Text;

namespace Power.EventBus.Person
{
    public class IntrusionEvent : DeviceEventWithData<IntrusionData>
    {
    }

    public class IntrusionData
    { 
    }
}
