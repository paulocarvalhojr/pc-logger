using PC.Logger;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    using Configuration;
    using Extensions;

    public static class ServiceCollectionLogConfigurationExtensions
    {
        public static IServiceCollection AddLogManager(this IServiceCollection services)
        {
            services.TryAddSingleton<ILogManager>(LogManager.CreateLoggerFactory(services));
            return services;
        }

        public static IServiceCollection AddLogSettings(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = new LogSettings();
            configuration.GetSection(LogSettings.LoggingSectionName).Bind(settings);
            return services.AddScoped(cfg => settings);
        }

        public static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration = null)
        {
            if (configuration != null)
                services.AddLogSettings(configuration);

            return services.AddLogManager();
        }
    }
}