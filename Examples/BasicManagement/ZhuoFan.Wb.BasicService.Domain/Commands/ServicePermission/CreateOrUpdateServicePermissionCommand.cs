using System;
using System.Collections.Generic;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.ServicePermission
{
    public class CreateOrUpdateServicePermissionCommand : Command
    {
        public CreateOrUpdateServicePermissionCommand(string serviceName, Guid serviceId, Dictionary<string, string> permissions)
        {
            ServiceName = serviceName;
            ServiceId = serviceId;
            Permissions = permissions;
        }

        public override string CommandDesc { get; set; } = "添加服务操作授权";

        public string ServiceName { get; private set; }
        public Guid ServiceId { get; private set; }
        public Dictionary<string, string> Permissions { get; private set; }
    }
}