﻿namespace Girvs.AutoMapper.Mapper;

public class AutoMapFromAttribute(Type entityType) : Attribute
{
    public Type EntityType { get; } = entityType;
}


public class AutoMapFromAttribute<TFromType> : AutoMapFromAttribute
{
    public AutoMapFromAttribute(Type entityType):base(typeof(TFromType))
    {
    }
}