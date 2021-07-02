namespace BasicManagement.Domain.Commands.SysDict
{
    public class DeleteSysDictCommand : SysDictCommand
    {
        public override string CommandDesc { get; set; } = "删除字典";

        public DeleteSysDictCommand(int id)
        {
            Id = id;
        }
    }
}
