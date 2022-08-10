namespace Girvs.Configuration;

public class ConfigurationDefaults
{
    // public static CacheKey SettingsAllAsDictionaryCacheKey => new CacheKey("Nop.setting.all.dictionary.", NopEntityCacheDefaults<Setting>.Prefix);

    public static string AppSettingsFilePath => "appsettings.json";
        
    public static string AppModelSettingsFilePath => "App_Data";

    public static string SerilogSettingFilePath => "Serilog.json";
}