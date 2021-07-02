namespace Girvs.Configuration
{
    public class ConfigurationDefaults
    {
        // public static CacheKey SettingsAllAsDictionaryCacheKey => new CacheKey("Nop.setting.all.dictionary.", NopEntityCacheDefaults<Setting>.Prefix);

        public static string AppSettingsFilePath => "App_Data/appsettings.json";
        
        public static string AppModelSettingsFilePath => "App_Data";

        public static string SerilogSettingFilePath => "App_Data/Serilog.json";
    }
}