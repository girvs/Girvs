using System;

namespace Girvs.EntityFrameworkCore.DbContextExtensions
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