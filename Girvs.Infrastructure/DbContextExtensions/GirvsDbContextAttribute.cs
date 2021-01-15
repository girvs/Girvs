using System;
using Girvs.Domain;

namespace Girvs.Infrastructure.DbContextExtensions
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GirvsDbContextAttribute : Attribute
    {
        public GirvsDbContextAttribute(string dbName)
        {
            DbName = dbName;
        }

        public string DbName { get; }
    }
}