namespace Girvs.Configuration;

public class CommonConfig : IConfig
{
    public bool DisplayFullErrorStack { get; set; } = false;
    public string Sm4SecretKey { get; set; } = string.Empty;
}