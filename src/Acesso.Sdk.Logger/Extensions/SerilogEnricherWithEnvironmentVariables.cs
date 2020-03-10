using System;
using System.Collections.Generic;
using PC.Logger.Enrichers;
using Serilog;
using Serilog.Configuration;

namespace PC.Logger.Extensions
{
    public static class SerilogEnricherWithEnvironmentVariablesExtensions
    {
        public static LoggerConfiguration WithEnvironmentVariables(
            this LoggerEnrichmentConfiguration enrichmentConfiguration, Dictionary<string, string> environmentVariableNames)
        {
            if (enrichmentConfiguration == null)
                throw new ArgumentNullException(nameof(enrichmentConfiguration));

            EnvironmentVariablesEnricher.Track(environmentVariableNames);
            return enrichmentConfiguration.With<EnvironmentVariablesEnricher>();
        }
    }
}
