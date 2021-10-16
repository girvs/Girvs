using System;
using System.Collections.Generic;
using Girvs.AuthorizePermission;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.ServicePermission
{
    public class CreateOrUpdateServicePermissionCommand : Command
    {
        public CreateOrUpdateServicePermissionCommand()
        {
  
        }

        public override string CommandDesc { get; set; } = "添加服务操作授权";

        public IList<ServicePermissionCommand> ServicePermissionCommands { get; protected set; }

    }



    public class ServicePermissionCommand
    {
        public string ServiceName { get;  set; }
        public Guid ServiceId { get;  set; }
        public Dictionary<string, string> Permissions { get;  set; }
        public List<OperationPermissionModel> OperationPermissionModels { get;  set; }
    }
}