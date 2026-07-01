namespace Girvs.Driven.Extensions;

using Microsoft.Extensions.DependencyInjection.Extensions;

public static class ServiceCollectionExtension
{
    public static void RegisterNotificationHandlerType(this IServiceCollection services)
    {
        var typeFinder = new WebAppTypeFinder();
        var types = typeFinder.FindOfType(typeof(INotificationHandler<>));
        foreach (var type in types)
        {
            // var implementedInterface = type.GetInterface("INotificationHandler`1");
            foreach (var implementedInterface in type.GetInterfaces())
            {
                services.AddScoped(implementedInterface, type);
            }
        }
    }

    public static void RegisterCommandHandlerType(this IServiceCollection services)
    {
        var typeFinder = new WebAppTypeFinder();
        var commandHandlerTypes = typeFinder.FindOfType<CommandHandler>()
            .Where(x => x.Name != nameof(CommandHandler));

        foreach (var commandHandlerType in commandHandlerTypes)
        {
            foreach (var @interface in commandHandlerType.GetInterfaces())
            {
                services.AddScoped(@interface, commandHandlerType);
            }
        }
    }


#if NET10_0_OR_GREATER
    public static IServiceCollection RegisterIValidatorType(this IServiceCollection services)
    {
        RegisterGirvsCommandValidators(services);
        return services;
    }

    #else
    public static void RegisterIValidatorType(this IServiceCollection services)
    {
        RegisterGirvsCommandValidators(services);
    }
#endif

    private static void RegisterGirvsCommandValidators(IServiceCollection services)
    {
        var typeFinder = new WebAppTypeFinder();

        var validatorTypes = typeFinder
            .FindOfType<IValidator>()
            .Where(type =>
                !type.IsAbstract &&
                !type.ContainsGenericParameters &&
                IsAssignableToOpenGeneric(type, typeof(GirvsCommandValidator<>)));

        foreach (var validatorType in validatorTypes)
        {
            var implementedInterface = validatorType.GetInterface("IValidator`1");
            if (implementedInterface != null)
            {
                services.AddScoped(implementedInterface, validatorType);
            }
        }
        // foreach (var validatorType in types)
        // {
        //     var parentType =
        //         ((System.Reflection.TypeInfo) validatorType).ImplementedInterfaces.FirstOrDefault(x =>
        //             x.Name == "IValidator`1");
        //     if (parentType != null)
        //     {
        //         services.AddScoped(parentType, validatorType);
        //     }
        // }
        // 兜底注册：没有具体校验器时使用 Default，不注册 FluentValidation 自身的开放泛型类型。
        services.TryAddScoped(typeof(IValidator<>), typeof(GirvsDefaultCommandValidator<>));
    }

    private static bool IsAssignableToOpenGeneric(Type type, Type openGeneric)
    {
        for (var currentType = type; currentType != null && currentType != typeof(object); currentType = currentType.BaseType)
        {
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == openGeneric)
                return true;
        }

        return false;
    }





}
