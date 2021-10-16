using System.Collections.Generic;
using System.Linq.Expressions;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Dto;

namespace ZhuoFan.Wb.BasicService.Application.ViewModels.User
{

    /// <summary>
    /// 数据规则授权模型
    /// </summary>
    public class UserDataRuleListViewModel
    {
        public UserDataRuleListViewModel()
        {
            DataRuleListFieldViewModels = new List<UserDataRuleListFieldViewModel>();
        }

        /// <summary>
        /// 实体说明
        /// </summary>
        public string EntityDesc { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// 实体需要相关授权的字段列表
        /// </summary>
        public List<UserDataRuleListFieldViewModel> DataRuleListFieldViewModels { get; set; }
    }


    public class UserDataRuleListFieldViewModel : IDto
    {
        public UserType UserType { get; set; }
        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 字段的说明
        /// </summary>
        public string FieldDesc { get; set; }

        /// <summary>
        /// 字段类型（预留）
        /// </summary>
        public string FieldType { get; set; }

        /// <summary>
        /// 字段赋值
        /// </summary>
        public string FieldValue { get; set; }
        
        /// <summary>
        /// 字段赋值文本
        /// </summary>
        public string FieldValueText { get; set; }

        /// <summary>
        /// 表达式运算符
        /// </summary>
        public ExpressionType ExpressionType { get; set; }
    }
}