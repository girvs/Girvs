namespace Girvs.Configuration.Resources;

public class Resource
{
    public string Type { get; set; }

    public Dictionary<string, string> Settings { get; set; } = new();
}
