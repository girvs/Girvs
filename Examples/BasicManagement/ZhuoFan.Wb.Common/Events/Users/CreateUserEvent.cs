﻿using System;
using System.ComponentModel;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.EventBus;

namespace ZhuoFan.Wb.Common.Events.Users
{
    public class CreateUserEvent : IntegrationEvent
    {
        public string UserAccount { get; set; }

        public string UserPassword { get; set; }

        public string UserName { get; set; }

        public string ContactNumber { get; set; }

        public UserType UserType { get; set; }
        
        public Guid TenantId { get; set; } 
    }
}
