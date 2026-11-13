using Newtonsoft.Json;

namespace Girvs.Configuration;

public class AppSettingsHelper
{
    public static async Task SaveAppSettingsAsync(AppSettings appSettings, IGirvsFileProvider fileProvider = null)
    {
        Singleton<AppSettings>.Instance = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        fileProvider ??= CommonHelper.DefaultFileProvider;

        //文件已存在则不再回写,只有首次(文件不存在)才创建并写入
        var filePath = fileProvider.MapPath(ConfigurationDefaults.AppSettingsFilePath);
        if (fileProvider.FileExists(filePath))
            return;

        fileProvider.CreateFile(filePath);

        //save app settings to the file
        var text = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
        await fileProvider.WriteAllTextAsync(filePath, text, Encoding.UTF8);
    }

    /// <summary>
    /// Save app settings to the file
    /// </summary>
    /// <param name="appSettings">App settings</param>
    /// <param name="fileProvider">File provider</param>
    public static void SaveAppSettings(AppSettings appSettings, IGirvsFileProvider fileProvider = null)
    {
        Singleton<AppSettings>.Instance = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        fileProvider ??= CommonHelper.DefaultFileProvider;

        //文件已存在则不再回写,只有首次(文件不存在)才创建并写入
        var filePath = fileProvider.MapPath(ConfigurationDefaults.AppSettingsFilePath);
        if (fileProvider.FileExists(filePath))
            return;

        fileProvider.CreateFile(filePath);

        //save app settings to the file
        var text = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
        fileProvider.WriteAllText(filePath, text, Encoding.UTF8);
    }

    public static bool ExistAppSettingsFile(IGirvsFileProvider fileProvider = null)
    {
        fileProvider ??= CommonHelper.DefaultFileProvider;
        var filePath = fileProvider.MapPath(ConfigurationDefaults.AppSettingsFilePath);
        return File.Exists(filePath);
    }
        
    public static void SaveAppModelConfigAsync(IAppModuleConfig appModelConfig, IGirvsFileProvider fileProvider = null)
    {
        fileProvider ??= CommonHelper.DefaultFileProvider;

        //create file if not exists
        var filePath = fileProvider.MapPath(ConfigurationDefaults.AppModelSettingsFilePath);
        filePath = fileProvider.Combine(filePath, string.Format("{0}.json", appModelConfig.GetType().Name));
        fileProvider.CreateFile(filePath);

        //save app settings to the file
        var text = JsonConvert.SerializeObject(appModelConfig, Formatting.Indented);
        fileProvider.WriteAllTextAsync(filePath, text, Encoding.UTF8);
    }

    public static void SaveAppModelConfig(IAppModuleConfig appModelConfig, IGirvsFileProvider fileProvider = null)
    {
        fileProvider ??= CommonHelper.DefaultFileProvider;

        //create file if not exists
        var filePath = fileProvider.MapPath(ConfigurationDefaults.AppModelSettingsFilePath);
        filePath = fileProvider.Combine(filePath, string.Format("{0}.json", appModelConfig.GetType().Name));
        fileProvider.CreateFile(filePath);

        //save app settings to the file
        var text = JsonConvert.SerializeObject(appModelConfig, Formatting.Indented);
        fileProvider.WriteAllText(filePath, text, Encoding.UTF8);
    }
}