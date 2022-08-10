namespace Girvs.Configuration;

public interface IConfig
{
    [JsonIgnore] string Name => GetType().Name;
}

public interface IAppModuleConfig : IConfig
{
    void Init();
}