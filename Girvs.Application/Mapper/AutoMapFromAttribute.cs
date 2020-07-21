using System;

namespace Girvs.Application.Mapper
{
    public class AutoMapFromAttribute : Attribute
    {
        public Type EntityType { get; }

        public AutoMapFromAttribute(Type entityType)
        {
            EntityType = entityType;
        }
    }
}