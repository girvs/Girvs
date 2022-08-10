namespace Girvs.EntityFrameworkCore.DbContextExtensions;

[AttributeUsage(AttributeTargets.Class)]
public class GirvsDbConfigAttribute : Attribute
{
    public GirvsDbConfigAttribute(string dbName)
    {
        DbName = dbName;
    }

    public string DbName { get; }
}