using System;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class UpdateUserRoleCommand : UserCommand
    {
        public UpdateUserRoleCommand(Guid id, Guid[] roleIds)
        {
            Id = id;
            RoleIds = roleIds;
        }

        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }
        
        public override string CommandDesc { get; set; } = "更新用户角色";
    }
}