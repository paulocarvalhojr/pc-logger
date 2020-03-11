using MassTransit;

namespace PC.Logger.Extensions
{
    public static class MassTransitLoggingConfigurerExtensions
    {
        public static void Logging(this IBusFactoryConfigurator configurer)
        {
            configurer.UseSerilog();
        }
    }
}
