using System;
using System.Text;
using System.Threading.Tasks;
using Girvs.FileProvider;
using Girvs.Infrastructure;
using Newtonsoft.Json;

namespace Girvs.Configuration
{
    public class AppSettingsHelper
    {
        public static async Task SaveAppSettingsAsync(AppSettings appSettings, IGirvsFileProvider fileProvider = null)
        {
            Singleton<AppSettings>.Instance = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

            fileProvider ??= CommonHelper.DefaultFileProvider;

            //create file if not exists
            var filePath = fileProvider.MapPath(ConfigurationDefaults.AppSettingsFilePath);
            fileProvider.CreateFile(filePath);

            //check additional configuration parameters
            // var additionalData = JsonConvert.DeserializeObject<AppSettings>(await fileProvider.ReadAllTextAsync(filePath, Encoding.UTF8))?.AdditionalData;
            // appSettings.AdditionalData = additionalData;

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

            //create file if not exists
            var filePath = fileProvider.MapPath(ConfigurationDefaults.AppSettingsFilePath);
            fileProvider.CreateFile(filePath);

            //check additional configuration parameters
            // var additionalData = JsonConvert.DeserializeObject<AppSettings>(fileProvider.ReadAllText(filePath, Encoding.UTF8))?.AdditionalData;
            // appSettings.AdditionalData = additionalData;

            //save app settings to the file
            var text = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
            fileProvider.WriteAllText(filePath, text, Encoding.UTF8);
        }
        
        public static void SaveAppModelConfigAsync(IAppModelConfig appModelConfig, IGirvsFileProvider fileProvider = null)
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

        public static void SaveAppModelConfig(IAppModelConfig appModelConfig, IGirvsFileProvider fileProvider = null)
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
}