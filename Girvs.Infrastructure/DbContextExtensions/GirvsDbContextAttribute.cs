using System;
using Girvs.Domain;

namespace Girvs.Infrastructure.DbContextExtensions
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GirvsDbContextAttribute : Attribute
    {
        public GirvsDbContextAttribute(string dbName, UseDataType useDataType)
        {
            DbName = dbName;
            UseDataType = useDataType;
        }

        public string DbName { get; }

        public UseDataType UseDataType { get; }

    }
}