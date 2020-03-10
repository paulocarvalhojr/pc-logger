using System;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace PC.Logger.Enrichers
{
    /// <inheritdoc />
    /// <summary>
    /// Enriquece os eventos de log com os valores de um conjunto pré-determinado de variáveis de ambiente.
    /// </summary>
    public class EnvironmentVariablesEnricher : ILogEventEnricher
    {
        private static Dictionary<string, string> TrackedEnvironmentVariableNames { get; set; } =
            new Dictionary<string, string>();

        private static Dictionary<string, EnvironmentVariableValueFallback> FallbackValueFactories { get; } =
            new Dictionary<string, EnvironmentVariableValueFallback>();

        private const string EnvironmentVariablesPropertyName = "Environment";

        private LogEventProperty _cachedEnvironmentVariablesProperty;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            EnsurePropertiesCreated(propertyFactory);
            EnsurePropertiesAdded(logEvent);
        }

        private void EnsurePropertiesCreated(ILogEventPropertyFactory propertyFactory)
        {
            _cachedEnvironmentVariablesProperty = _cachedEnvironmentVariablesProperty ??
                                                  propertyFactory.CreateProperty(EnvironmentVariablesPropertyName,
                                                      GetTrackedEnvironmentVariables(), true);
        }

        private void EnsurePropertiesAdded(LogEvent logEvent)
        {
            logEvent.AddPropertyIfAbsent(_cachedEnvironmentVariablesProperty);
        }

        private Dictionary<string, string> GetTrackedEnvironmentVariables()
        {
            var result = new Dictionary<string, string>();
            foreach (var envVarName in TrackedEnvironmentVariableNames.Keys)
            {
                var envVarValue = (string) null;
                var logPropName = TrackedEnvironmentVariableNames[envVarName];
                if (string.IsNullOrWhiteSpace(logPropName))
                    continue;

                try
                {
                    envVarValue = Environment.GetEnvironmentVariable(envVarName) ?? string.Empty;
                }
                catch(Exception ex)
                {
                    Serilog.Debugging.SelfLog.WriteLine("Failed to read a tracked environment variable value.",
                        envVarName, ex);
                }

                if (FallbackValueFactories.TryGetValue(envVarName, out var fallbackValueFactory))
                    envVarValue = fallbackValueFactory.ValueValidator?.Invoke(envVarValue) ?? false
                        ? fallbackValueFactory.FallbackValueGenerator?.Invoke() ?? envVarValue
                        : envVarValue;

                if (envVarValue != null)
                    result[logPropName] = envVarValue;
            }

            return result;
        }

        /// <summary>
        /// Define a lista de variáveis de ambiente que deverão ser inseridas nos registros de log.
        /// </summary>
        /// <param name="environmentVariableNamesToTrack"></param>
        public static void Track(Dictionary<string, string> environmentVariableNamesToTrack)
        {
            TrackedEnvironmentVariableNames = environmentVariableNamesToTrack ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Registra uma regra de fallback para definição do valor de uma variável de ambiente.
        /// </summary>
        /// <param name="fallback"></param>
        // ReSharper disable once UnusedMember.Global
        public static void RegisterFallback(EnvironmentVariableValueFallback fallback)
        {
            if (string.IsNullOrWhiteSpace(fallback?.VariableName))
                return;

            FallbackValueFactories[fallback.VariableName] = fallback;
        }

        public class EnvironmentVariableValueFallback
        {
            public string VariableName { get; }

            public Func<string, bool> ValueValidator { get; set; }

            public Func<string> FallbackValueGenerator { get; set; }

            public EnvironmentVariableValueFallback(string variableName)
            {
                VariableName = variableName;
            }
        }
    }
}
