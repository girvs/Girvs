using BasicManagement.Domain.Commands.SysDict;

namespace BasicManagement.Domain.Validations.SysDict
{
    public class CreateSysDictCommandValidation: SysDictCommandValidation<CreateSysDictCommand>
    {
        public CreateSysDictCommandValidation()
        {
            ValidationCode();
            ValidationCodeType();
            ValidationName();
            ValidationDesc();
        }
    }
}