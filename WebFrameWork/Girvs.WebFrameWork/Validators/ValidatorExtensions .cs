using FluentValidation;

namespace Girvs.WebFrameWork.Validators
{
    /// <summary>
    /// ��֤������չ
    /// </summary>
    public static class ValidatorExtensions
    {
        /// <summary>
        /// �������ÿ���֤��
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="ruleBuilder">RuleBuilder</param>
        /// <returns>Result</returns>
        public static IRuleBuilderOptions<T, string> IsCreditCard<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new CreditCardPropertyValidator());
        }

        /// <summary>
        /// ����ʮ������֤��
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="ruleBuilder">RuleBuilder</param>
        /// <param name="maxValue">Maximum value</param>
        /// <returns>Result</returns>
        public static IRuleBuilderOptions<T, decimal> IsDecimal<T>(this IRuleBuilder<T, decimal> ruleBuilder, decimal maxValue)
        {
            return ruleBuilder.SetValidator(new DecimalPropertyValidator(maxValue));
        }

        /// <summary>
        /// ʵʩ������֤��
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="ruleBuilder">RuleBuilder</param>
        /// <param name="localizationService">Localization service</param>
        /// <param name="customerSettings">Customer settings</param>
        /// <returns>Result</returns>
        public static IRuleBuilder<T, string> IsPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            var regExp = "^";
            //Passwords must be at least X characters and contain the following: upper case (A-Z), lower case (a-z), number (0-9) and special character (e.g. !@#$%^&*-)
            regExp += "(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{{8,}}$";

            var options = ruleBuilder
                .NotEmpty().WithMessage("Validation.Password")
                .Matches(regExp).WithMessage("Validation.Password");

            return options;
        }
    }
}