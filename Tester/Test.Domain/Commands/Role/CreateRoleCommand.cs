namespace Test.Domain.Commands.Role
{
    public class CreateRoleCommand : RoleCommand
    {
        public CreateRoleCommand(string name,string desc)
        {
            Name = name;
            Desc = desc;
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}