using System;
using System.Collections.Generic;
using Girvs.Domain.Enumerations;

namespace Girvs.Application.Dtos
{
    public class ServiceMethodPermissionListDto : IDto
    {
        public ServiceMethodPermissionListDto()
        {
            Permissions = new Dictionary<string, string>();
        }

        public string ServiceName { get; set; }

        public Guid ServiceId { get; set; }
        public Dictionary<string, string> Permissions { get; set; }
    }
}