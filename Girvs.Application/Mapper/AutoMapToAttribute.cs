using System;

namespace Girvs.Application.Mapper
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