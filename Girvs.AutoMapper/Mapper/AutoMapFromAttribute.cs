namespace Girvs.AutoMapper.Mapper;

public class AutoMapFromAttribute : Attribute
{
    public Type EntityType { get; }

    public AutoMapFromAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}