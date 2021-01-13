namespace Test.Domain.Commands.Role
{
    public class CreateRoleCommand : RoleCommand
    {
        public CreateRoleCommand(string name,string desc)
        {
            Name = name;
            Desc = desc;
        }

        public override string CommandDesc { get; set; } = "创建角色";

        public override bool IsValid()
        {
            return true;
        }
    }
}