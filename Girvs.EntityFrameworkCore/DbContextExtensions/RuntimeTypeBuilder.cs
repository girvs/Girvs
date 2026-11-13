using System.Text;

namespace Girvs.EntityFrameworkCore.DbContextExtensions;

internal static class RuntimeTypeBuilder
{
    private static readonly ModuleBuilder ModuleBuilder;
    private static readonly Dictionary<string, Type> BuiltTypes;
    private static readonly object SyncRoot = new();

    static RuntimeTypeBuilder()
    {
        var assemblyName = new AssemblyName {Name = "DynamicLinqTypes"};

        ModuleBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(assemblyName.Name);
        BuiltTypes = new Dictionary<string, Type>();
    }

    internal static Type GetRuntimeType(IDictionary<string, PropertyInfo> fields)
    {
        var typeKey = GetTypeKey(fields);
        lock (SyncRoot)
        {
            if (!BuiltTypes.TryGetValue(typeKey, out var builtType))
            {
                builtType = GetRuntimeType(typeKey, fields);
                BuiltTypes[typeKey] = builtType;
            }

            return builtType;
        }
    }

    private static Type GetRuntimeType(string typeName, IEnumerable<KeyValuePair<string, PropertyInfo>> properties)
    {
        // 该动态类型只用于 LINQ 投影，不参与 Formatter 序列化；.NET 11 中 TypeAttributes.Serializable 已过时（SYSLIB0050）。
        var typeBuilder = ModuleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
        foreach (var property in properties)
        {
            typeBuilder.DefineField(property.Key, property.Value.PropertyType, FieldAttributes.Public);
        }

        return typeBuilder.CreateType();
    }

    private static string GetTypeKey(IEnumerable<KeyValuePair<string, PropertyInfo>> fields)
    {
        var builder = new StringBuilder();
        foreach (var field in fields)
        {
            builder.Append(field.Key).Append(';').Append(field.Value.Name).Append(';');
        }

        return builder.ToString();
    }
}