using BasicManagement.Domain.Commands.SysDict;

namespace BasicManagement.Domain.Validations.SysDict
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