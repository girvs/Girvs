using System;
using System.Linq.Expressions;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Queries;
using Girvs.Extensions;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Domain.Queries
{
    public class PermissionQuery : QueryBase<BasalPermission>
    {
        /// <summary>
        /// 功能模块的ID
        /// </summary>
        public Guid? AppliedObjectID { get; set; }
        /// <summary>
        /// 接受权限类型(用户或者角色)
        /// </summary>
        public PermissionAppliedObjectType AppliedType = PermissionAppliedObjectType.Role;
        /// <summary>
        /// 用户或角色ID
        /// </summary>
        public Guid? AppliedID { get; set; }

        /// <summary>
        /// 权限分类(比如说功能菜单,数据记录等)
        /// </summary>
        public PermissionValidateObjectType ValidateObjectType = PermissionValidateObjectType.FunctionMenu;


        public override Expression<Func<BasalPermission, bool>> GetQueryWhere()
        {
            Expression<Func<BasalPermission, bool>> expression = x => x.ValidateObjectType == ValidateObjectType && x.AppliedObjectType == AppliedType;

            if (AppliedID.HasValue)
            {
                expression = expression.And(x => x.AppliedID == AppliedID.Value);
            }

            if (AppliedObjectID.HasValue)
            {
                expression = expression.And(x => x.AppliedObjectID == AppliedID.Value);
            }

            return expression;
        }
    }
}
