namespace Girvs.AutoMapper.Mapper;

public class AutoMapToAttribute(Type entityType) : Attribute
{
    public Type EntityType { get; } = entityType;
}


public class AutoMapToAttribute<TToType> : AutoMapFromAttribute
{
    public AutoMapToAttribute(Type entityType):base(typeof(TToType))
    {
    }
}