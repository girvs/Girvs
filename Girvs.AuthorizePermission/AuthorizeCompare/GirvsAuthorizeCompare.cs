namespace Girvs.AuthorizePermission.AuthorizeCompare;

public abstract class GirvsAuthorizeCompare : GirvsRepositoryOtherQueryCondition, IServiceMethodPermissionCompare
{
    public abstract AuthorizeModel GetCurrnetUserAuthorize();

    /// <summary>
    /// 判断当前实体是否包含数据校验规则，不包含则直接跳过
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public virtual bool IsIncludeVerifyDataRuleByEntity(Type entityType)
    {
        var properties = entityType.GetProperties();
        return properties.Select(propertyInfo => propertyInfo.GetCustomAttribute<DataRuleAttribute>())
            .Any(dataRule => dataRule != null);
    }

    /// <summary>
    /// 判断当前用户是否登陆
    /// </summary>
    /// <returns></returns>
    public virtual bool IsLogin()
    {
        //return (EngineContext.Current.ClaimManager?.CurrentClaims ?? Array.Empty<Claim>()).Any();
        var httpContext = EngineContext.Current.HttpContext;
        return httpContext?.User.Identity != null
               && httpContext.User.Identity.IsAuthenticated;
    }

    public override Expression<Func<TEntity, bool>> GetOtherQueryCondition<TEntity>()
    {
        //如果当前用户没有登陆，则跳过
        if (!IsLogin())
        {
            return x => true;
        }

        //默认判断如果存
        var expression = base.GetOtherQueryCondition<TEntity>();

        //当前实体不包含数据权限标识，跳过
        if (!IsIncludeVerifyDataRuleByEntity(typeof(TEntity)))
        {
            return expression;
        }

        //如果是前台或者事件，只添加租户判断
        var identityType = EngineContext.Current.ClaimManager.IdentityClaim.IdentityType;
        if (identityType is IdentityType.RegisterUser or IdentityType.EventMessageUser)
        {
            return expression;
        }

        //如果登陆的是系统管理员或者是租户管理员，则只返回租户条件，默认为所有的数据权限
        var userType = EngineContext.Current.ClaimManager.GetUserType();
        if (userType is UserType.AdminUser or UserType.TenantAdminUser)
        {
            return expression;
        }

        var currentUserAuthorize = GetCurrnetUserAuthorize() ??
                                   new AuthorizeModel(new List<AuthorizeDataRuleModel>(),
                                       new List<AuthorizePermissionModel>());
        
        var dataRuleModels = currentUserAuthorize.AuthorizeDataRules;

        if (dataRuleModels == null)
        {
            throw new GirvsException("未获取相关的数据授权信息", 568);
        }

        var currentEntityDataRule =
            dataRuleModels.FirstOrDefault(x => x.EntityTypeName == typeof(TEntity).FullName);

        // 如果用户没有设置数据权限，则直接抛出异常,并且配置设置用户不是默认为所有数据
        if (currentEntityDataRule == null || !currentEntityDataRule.AuthorizeDataRuleFieldModels.Any())
        {
            var config = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();
            if (!config.UserDataRuleDefaultAll)
            {
                throw new GirvsException("未配置当前用户对该模块的数据权限，请先获取权限", 568);
            }
        }

        if (currentEntityDataRule != null)
        {
            //临时解决方案 查找出所有为Or的字段条件
            var orFields = GetEntityDataRuleOrFields(typeof(TEntity));
            //先处理Or 的字段条件
            var orAuthorizeDataRuleFieldModels =
                currentEntityDataRule.AuthorizeDataRuleFieldModels.Where(x => orFields.Contains(x.FieldName));

            var orExpression = SpliceCondition<TEntity>(orAuthorizeDataRuleFieldModels, ConditionType.Or);
            expression = expression.And(orExpression);

            //再处理And的字段条件

            var andAuthorizeDataRuleFieldModels =
                currentEntityDataRule.AuthorizeDataRuleFieldModels.Where(x => !orFields.Contains(x.FieldName));

            var andExpression = SpliceCondition<TEntity>(andAuthorizeDataRuleFieldModels, ConditionType.And);
            expression = expression.And(andExpression);
        }

        return expression;
    }

    #region 获取实体中所有带Or的条件字段

    private Dictionary<string, string[]> entityDataRuleOrFields = new Dictionary<string, string[]>();
    private static object sync = new object();

    private string[] GetEntityDataRuleOrFields(Type entityType)
    {
        lock (sync)
        {
            if (!entityDataRuleOrFields.ContainsKey(entityType.Name))
            {
                var orFields = entityType.GetProperties().Where(x =>
                {
                    var dataRule = x.GetCustomAttribute<DataRuleAttribute>();
                    if (dataRule != null)
                    {
                        return dataRule.ConditionType == ConditionType.Or;
                    }

                    return false;
                }).Select(x => x.Name).ToArray();
                entityDataRuleOrFields.Add(entityType.Name, orFields);
            }

            return entityDataRuleOrFields[entityType.Name];
        }
    }

    #endregion

    private Expression<Func<TEntity, bool>> SpliceCondition<TEntity>(
        IEnumerable<AuthorizeDataRuleFieldModel> dataRuleFieldModels, ConditionType conditionType)
    {
        Expression<Func<TEntity, bool>> innerExpression = x => conditionType == ConditionType.And;
        foreach (var dataRuleFieldModel in dataRuleFieldModels)
        {
            if (string.IsNullOrEmpty(dataRuleFieldModel.FieldValue))
                continue;

            var fieldValues = dataRuleFieldModel.FieldValue.Split(',');
            var ex = BuilderBinaryExpression<TEntity>(
                dataRuleFieldModel.FieldName,
                dataRuleFieldModel.FieldType,
                dataRuleFieldModel.ExpressionType,
                fieldValues);

            innerExpression = conditionType == ConditionType.And
                ? innerExpression.And(ex)
                : innerExpression.Or(ex);
        }

        return innerExpression;
    }

    // private List<object> ConverFieldValueToArray(string fieldType, string fieldValue)
    // {
    //     var values = fieldValue.Split(",");
    //     return values.Select(value => GirvsConvert.ToSpecifiedType(fieldType, value)).ToList();
    // }

    public virtual bool PermissionCompare(Guid functionId, Permission permission)
    {
        var currentUserAuthorize = GetCurrnetUserAuthorize() ?? new AuthorizeModel(new List<AuthorizeDataRuleModel>(),
            new List<AuthorizePermissionModel>());

        var ps = currentUserAuthorize.AuthorizePermissions;

        if (ps == null)
        {
            throw new GirvsException("未获取相关的功能授权信息", 568);
        }

        var key = permission.ToString();
        return ps.Any(x => x.ServiceId == functionId && x.Permissions.ContainsValue(key));
    }
}