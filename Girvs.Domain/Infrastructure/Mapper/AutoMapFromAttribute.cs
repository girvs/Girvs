using System;

namespace Girvs.Domain.Infrastructure.Mapper
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