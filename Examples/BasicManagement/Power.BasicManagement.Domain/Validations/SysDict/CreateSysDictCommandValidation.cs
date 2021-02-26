using Power.BasicManagement.Domain.Commands.SysDict;

namespace Power.BasicManagement.Domain.Validations.SysDict
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