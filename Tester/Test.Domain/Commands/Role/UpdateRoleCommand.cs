namespace Test.Domain.Commands.Role
{
    public class UpdateRoleCommand : RoleCommand
    {
        public UpdateRoleCommand(string name,string desc)
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