using System;
using System.Collections.Generic;
using Girvs.BusinessBasis.Entities;

namespace ZhuoFan.Wb.BasicService.Domain.Models
{
    public class ServicePermission : AggregateRoot<Guid>
    {
        public string ServiceName { get; set; }
        public Guid ServiceId { get; set; }
        public Dictionary<string, string> Permissions { get; set; }
    }
}