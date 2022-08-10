namespace Girvs.BusinessBasis.QueryTypeFields;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class QuerySourceMemberAttribute: Attribute
{
    public QuerySourceMemberAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; private set; }
}