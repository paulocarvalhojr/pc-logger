using PC.Shared;

namespace PC.Logger.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public static class LogManagerConfigurationExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logManager"></param>
        /// <param name="globalLogSettings"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static ILogManager ConfigureLogging(this ILogManager logManager,
            ILogOptions globalLogSettings)
        {
            Ensure.Argument.NotNull(logManager, nameof(logManager));

            logManager.Configure(globalLogSettings);

            return logManager;
        }
    }
}