namespace Girvs.Configuration.Resources;

public class GirvsInfrastructureResource
{
    public string Type { get; set; }

    public Dictionary<string, string> Settings { get; set; } = new();
}
