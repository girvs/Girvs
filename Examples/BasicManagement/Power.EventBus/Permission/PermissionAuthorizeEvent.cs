using System;
using System.Collections.Generic;

namespace Power.EventBus.Permission
{
    public class PermissionAuthorize
    {
        public PermissionAuthorize()
        {
            Permissions = new Dictionary<string, string>();
        }

        public string ServiceName { get; set; }
        public Guid ServiceId { get; set; }
        public Dictionary<string, string> Permissions { get; set; }
    }



    public class PermissionAuthorizeEvent : IntegrationEvent
    {
        public PermissionAuthorizeEvent()
        {
            PermissionAuthorizes = new List<PermissionAuthorize>();
        }

        public List<PermissionAuthorize> PermissionAuthorizes { get; set; }
    }
}