using Microsoft.Extensions.Configuration;

namespace PC.Logger.Extensions
{
    public static class ConfigurationLogSettingsExtensions
    {
        public static LogSettings GetLogSettings(this IConfiguration configuration)
        {
            return configuration.GetSection(LogSettings.LoggingSectionName).Get<LogSettings>() ?? new LogSettings();
        }
    }
}