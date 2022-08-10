namespace Girvs.Infrastructure;

public interface IDependencyRegistrar
{
    void Register(IServiceCollection services, ITypeFinder typeFinder, AppSettings appSettings);
    int Order { get; }
}