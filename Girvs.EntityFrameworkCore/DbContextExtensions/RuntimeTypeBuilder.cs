namespace Girvs.EntityFrameworkCore.DbContextExtensions;

internal static class RuntimeTypeBuilder
{
    private static readonly ModuleBuilder ModuleBuilder;
    private static readonly IDictionary<string, Type> BuiltTypes;

    static RuntimeTypeBuilder()
    {
        var assemblyName = new AssemblyName {Name = "DynamicLinqTypes"};

        ModuleBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(assemblyName.Name);
        //moduleBuilder = Thread.GetDomain()
        //        .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
        //        .DefineDynamicModule(assemblyName.Name);
        BuiltTypes = new Dictionary<string, Type>();
    }

    internal static Type GetRuntimeType(IDictionary<string, PropertyInfo> fields)
    {
        var typeKey = GetTypeKey(fields);
        if (!BuiltTypes.ContainsKey(typeKey))
        {
            lock (ModuleBuilder)
            {
                BuiltTypes[typeKey] = GetRuntimeType(typeKey, fields);
            }
        }

        return BuiltTypes[typeKey];
    }

    private static Type GetRuntimeType(string typeName, IEnumerable<KeyValuePair<string, PropertyInfo>> properties)
    {
        var typeBuilder = ModuleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);
        foreach (var property in properties)
        {
            typeBuilder.DefineField(property.Key, property.Value.PropertyType, FieldAttributes.Public);
        }

        return typeBuilder.CreateType();
    }

    private static string GetTypeKey(IEnumerable<KeyValuePair<string, PropertyInfo>> fields)
    {
        return fields.Aggregate(string.Empty, (current, field) => current + (field.Key + ";" + field.Value.Name + ";"));
    }
}