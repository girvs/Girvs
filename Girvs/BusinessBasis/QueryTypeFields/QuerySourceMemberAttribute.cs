namespace Girvs.BusinessBasis.QueryTypeFields;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class QuerySourceMemberAttribute(string name) : Attribute
{
    public string Name { get; private set; } = name;
}
