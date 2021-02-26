using System;
using FluentValidation;
using Girvs.Domain.Driven.Validations;
using Power.BasicManagement.Domain.Commands.User;

namespace Power.BasicManagement.Domain.Validations.User
{
    public abstract class UserCommandValidation<TCommand> : GirvsCommandValidator<TCommand> where TCommand : UserCommand
    {
        //验证Guid
        protected virtual void ValidateOtherId()
        {
            RuleFor(c => c.OtherId)
                .NotEqual(Guid.Empty);
        }
        
        protected virtual void ValidationName()
        {
            RuleFor(user => user.UserName)
                .NotEmpty().WithMessage("用户名称不能为空")
                .MinimumLength(2).WithMessage("用户名称长度不能小于2")
                .MaximumLength(12).WithMessage("用户名称长度不能大于12");
        }
        
        protected virtual void ValidationUserAccount()
        {
            RuleFor(user => user.UserAccount)
                .NotEmpty().WithMessage("用户登陆名称不能为空")
                .MinimumLength(6).WithMessage("用户登陆名称长度不能小于6")
                .MaximumLength(12).WithMessage("用户登陆名称长度不能大于12");
        }
        
        protected virtual void ValidationContactNumber()
        {
            RuleFor(user => user.ContactNumber)
                .NotEmpty().WithMessage("用户手机号码不能为空")
                .Length(11).WithMessage("手机号码长度必须是11位");
        }
        
        protected virtual void ValidationUserPassword()
        {
            RuleFor(user => user.UserPassword)
                .NotEmpty().WithMessage("用户登陆密码不能为空")
                .MinimumLength(6).WithMessage("用户登陆密码长度不能小于6");
        }
        
        protected virtual void ValidationNewPassword()
        {
            RuleFor(user => user.NewPassword)
                .NotEmpty().WithMessage("用户新的登陆密码不能为空")
                .MinimumLength(6).WithMessage("用户新的登陆密码长度不能小于6");
        }
        
        protected virtual void ValidationOldPassword()
        {
            RuleFor(user => user.OldPassword)
                .NotEmpty().WithMessage("用户旧的登陆密码不能为空");
        }
    }
}