using BasicManagement.Domain.Commands.SysDict;
using FluentValidation;
using Girvs.Driven.Validations;

namespace BasicManagement.Domain.Validations.SysDict
{
    public abstract class SysDictCommandValidation<TCommand> : GirvsCommandValidator<TCommand> where TCommand : SysDictCommand
    {
        protected virtual void ValidationName()
        {
            RuleFor(d => d.Name)
                .NotEmpty().WithMessage("字典名称不能为空")
                .MinimumLength(2).WithMessage("字典名称长度不能小于2")
                .MaximumLength(12).WithMessage("字典名称长度不能大于36");
        }

        protected virtual void ValidationCodeType()
        {
            RuleFor(d => d.CodeType)
                .NotEmpty().WithMessage("字典分类不能为空")
                .MinimumLength(2).WithMessage("字典分类长度不能小于2")
                .MaximumLength(12).WithMessage("字典分类长度不能大于36");
        }

        protected virtual void ValidationCode()
        {
            RuleFor(d => d.Code)
                .NotEmpty().WithMessage("字典编号不能为空")
                .MinimumLength(2).WithMessage("字典编号长度不能小于2")
                .MaximumLength(12).WithMessage("字典编号长度不能大于36");
        }

        protected virtual void ValidationDesc()
        {
            RuleFor(d => d.Name)
                .MaximumLength(12).WithMessage("字典描述长度不能大于200");
        }
    }
}