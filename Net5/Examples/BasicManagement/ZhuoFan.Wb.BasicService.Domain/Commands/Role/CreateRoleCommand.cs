using System;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.Role
{
    public class CreateRoleCommand : RoleCommand
    {
        public CreateRoleCommand(string name,string desc,Guid[] userGuids)
        {
            Name = name;
            Desc = desc;
            UserIds = userGuids;
        }
        
        public override string CommandDesc { get; set; } = "创建角色";

        public override bool IsValid()
        {
            return true;
        }
    }
}