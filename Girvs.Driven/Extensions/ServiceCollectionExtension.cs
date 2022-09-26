namespace Girvs.Driven.Extensions;

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


    public static void RegisterIValidatorType(this IServiceCollection services)
    {
        var typeFinder = new WebAppTypeFinder();

        var validatorTypes = typeFinder.FindOfType<IValidator>();

        foreach (var validatorType in validatorTypes)
        {
            if (validatorType.FullName == typeof(GirvsDefaultCommandValidator<>).FullName)
            {
                continue;
            }
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
    }
}