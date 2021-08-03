using System;

namespace Girvs.Refit
{
    public class RefitServiceAttribute : Attribute
    {
        public RefitServiceAttribute(string serviceName, bool inConsul = true)
        {
            ServiceName = serviceName;
            InConsul = inConsul;
        }

        public string ServiceName { get; private set; }
        public bool InConsul { get; private set; }
    }
}