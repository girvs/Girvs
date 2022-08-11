namespace Girvs.AuthorizePermission;

/// <summary>
/// 数据规则授权模型
/// </summary>
/// <param name="EntityDesc">实体说明</param>
/// <param name="EntityTypeName">服务名称</param>
/// <param name="Tag">所属标签</param>
/// <param name="Order">排序</param>
/// <param name="AuthorizeDataRuleFieldModels">实体需要相关授权的字段列表</param>
public record AuthorizeDataRuleModel(string EntityDesc, string EntityTypeName, string Tag, int Order,
    List<AuthorizeDataRuleFieldModel> AuthorizeDataRuleFieldModels) : IDto;

/// <summary>
/// 数据权限规则字段授权模型
/// </summary>
/// <param name="UserType">授权用户类型</param>
/// <param name="FieldName">字段名称</param>
/// <param name="FieldDesc">字段的说明</param>
/// <param name="FieldType">字段类型（预留）</param>
/// <param name="FieldValue">字段赋值</param>
/// <param name="ExpressionType">表达式运算符</param>
public record AuthorizeDataRuleFieldModel(UserType UserType, string FieldName, string FieldDesc, string FieldType,
    string FieldValue, ExpressionType ExpressionType) : IDto;