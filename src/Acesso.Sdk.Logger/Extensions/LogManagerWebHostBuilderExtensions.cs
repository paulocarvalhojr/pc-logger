#if (NETSTANDARD2_0)
using Serilog;
using Microsoft.AspNetCore.Hosting;

namespace PC.Logger.Extensions
{
    public static class LogManagerWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseLogger(this IWebHostBuilder builder)
        {
            return builder.UseSerilog();
        }
    }
}
#endif