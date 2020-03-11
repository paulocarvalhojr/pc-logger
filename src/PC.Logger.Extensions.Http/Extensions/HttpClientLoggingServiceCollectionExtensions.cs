#if NETSTANDARD2_0
// ReSharper disable UnusedMember.Global
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    using Extensions;
    using Http;
    using PC.Logger.Extensions.Http;
    using System;

    public static class HttpClientLoggingServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpClientLogging(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddEnumerable(ServiceDescriptor
                .Singleton<IHttpMessageHandlerBuilderFilter, LoggingMessageHandlerBuilderFilter>());

            return services;
        }
    }
}
#endif