using System;
using System.Collections.Generic;
using System.Text;

namespace Power.BasicManagement.Domain.Commands.SysDict
{
    public class UpdateSysDictCommand : SysDictCommand
    {
        public UpdateSysDictCommand(int id, string name, string desc, string code, string codeType)
        {
            Id = id;
            Name = name;
            Desc = desc;
            Code = code;
            CodeType = codeType;
        }
        public override string CommandDesc { get; set; } = "更新字典";
    }
}
