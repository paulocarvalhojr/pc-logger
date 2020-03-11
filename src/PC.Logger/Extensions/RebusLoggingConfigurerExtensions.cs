using System;
using System.Collections.Generic;
using System.Text;
using Rebus.Config;

namespace PC.Logger.Extensions
{
    public static class RebusLoggingConfigurerExtensions
    {
        public static void Logging(this RebusLoggingConfigurer configurer)
        {
            configurer.Serilog();
        }
    }
}
