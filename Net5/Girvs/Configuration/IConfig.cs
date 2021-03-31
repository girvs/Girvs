using System.Text.Json.Serialization;

namespace Girvs.Configuration
{
    public interface IConfig
    {
        [JsonIgnore] string Name => GetType().Name;
    }
}