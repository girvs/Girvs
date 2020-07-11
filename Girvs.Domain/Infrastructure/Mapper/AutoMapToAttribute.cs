using System;

namespace Girvs.Domain.Infrastructure.Mapper
{
    public class AutoMapToAttribute : Attribute
    {
        public Type EntityType { get; }

        public AutoMapToAttribute(Type entityType)
        {
            EntityType = entityType;
        }
    }
}