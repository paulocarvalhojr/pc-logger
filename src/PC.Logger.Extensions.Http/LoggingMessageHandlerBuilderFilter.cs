#if NETSTANDARD2_0
namespace PC.Logger.Extensions.Http
{
    using System;
    using Microsoft.Extensions.Http;
    using Microsoft.Extensions.Logging;

    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public class LoggingMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// </summary>
        /// <param name="loggerFactory"></param>
        // ReSharper disable once UnusedMember.Global
        public LoggingMessageHandlerBuilderFilter(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            return builder =>
            {
                // Run other configuration first, we want to decorate.
                next(builder);

                // Add our custom logging handler into the handler factory.
                builder.AdditionalHandlers.Add(LoggingHttpMessageHandler.Create(builder.Name, _loggerFactory));
            };
        }
    }
}
#endif