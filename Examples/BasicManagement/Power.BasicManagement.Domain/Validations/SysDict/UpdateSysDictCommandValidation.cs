using Power.BasicManagement.Domain.Commands.SysDict;

namespace Power.BasicManagement.Domain.Validations.SysDict
{
    public class UpdateSysDictCommandValidation: SysDictCommandValidation<UpdateSysDictCommand>
    {
        public UpdateSysDictCommandValidation()
        {
            ValidationName();
            ValidationCode();
            ValidationCodeType();
            ValidationDesc();
        }
    }
}