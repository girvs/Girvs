﻿using FluentValidation;
using Girvs.Domain.Driven.Validations;
using Power.BasicManagement.Domain.Commands.Role;

namespace Power.BasicManagement.Domain.Validations.Role
{
    public abstract class RoleCommandValidation<TCommand>: GirvsCommandValidator<TCommand> where TCommand : RoleCommand
    {
        protected virtual void ValidationName()
        {
            RuleFor(role => role.Name)
                .NotEmpty().WithMessage("角色名称不能为空")
                .MinimumLength(2).WithMessage("角色名称长度不能小于2")
                .MaximumLength(12).WithMessage("角色名称长度不能大于12");
        }
    }
}