namespace BasicManagement.Domain.Commands.SysDict
{
    public class CreateSysDictCommand : SysDictCommand
    {
        public CreateSysDictCommand(string name, string desc, string code, string codeType)
        {
            Name = name;
            Desc = desc;
            Code = code;
            CodeType = codeType;
        }
        public override string CommandDesc { get; set; } = "创建字典";
    }
}
