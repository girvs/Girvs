namespace Test.Domain.Commands.Role
{
    public class UpdateRoleCommand : RoleCommand
    {
        public UpdateRoleCommand(string name,string desc)
        {
            Name = name;
            Desc = desc;
        }

        public override string CommandDesc { get; set; } = "更新角色";

        public override bool IsValid()
        {
            return true;
        }
    }
}