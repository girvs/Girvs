namespace Girvs.AuthorizePermission;

public class DataRuleAttribute(
    string attributeDesc,
    UserType userType = UserType.All,
    string tag = "",
    int order = 0,
    ConditionType conditionType = ConditionType.Or
) : Attribute
{
    /// <summary>
    /// 标记说明
    /// </summary>
    public string AttributeDesc { get; private set; } = attributeDesc;

    public UserType UserType { get; private set; } = userType;

    /// <summary>
    /// 所属标签
    /// </summary>
    public string Tag { get; private set; } = tag;

    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; private set; } = order;

    /// <summary>
    /// 条件类型
    /// </summary>
    public ConditionType ConditionType { get; private set; } = conditionType;
}

public enum ConditionType
{
    And,
    Or,
}
