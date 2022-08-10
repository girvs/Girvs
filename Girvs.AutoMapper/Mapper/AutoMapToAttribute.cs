namespace Girvs.AutoMapper.Mapper;

public class AutoMapToAttribute : Attribute
{
    public Type EntityType { get; }

    public AutoMapToAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}