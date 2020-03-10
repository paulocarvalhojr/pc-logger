using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using PC.Logger.Enrichers.HttpContext;

namespace PC.Logger.Extensions
{
    public static class ApplicationBuilderUseLogContextExtensions
    {
        public static IApplicationBuilder UseLogContext(this IApplicationBuilder builder,
            Action<LogContextMiddlewareOptions> settings)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var options = new LogContextMiddlewareOptions();
            settings(options);
            return builder.UseMiddleware<LogContextMiddleware>(Options.Create(options));
        }
    }
}