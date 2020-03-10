using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.Email;
using Serilog.Sinks.Slack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using PC.Logger.Extensions;
using PC.Shared;

namespace PC.Logger
{
    public partial class LogManager : ILogManager
    {
        private enum LogSinkType
        {
            None,
            MongoDb,
            Slack,
            Email,
            Seq,
            ElasticSearch
        }

        public static Dictionary<string, string> EnvironmentVariablesToLog { get; set; } =
            new Dictionary<string, string>
            {
                {"ENVIRONMENT", "Type"},
                {"APPLICATION_NAME", "ApplicationName"}
            };

        public static string TransactionIdLogPropertyName { get; set; } = "CorrelationId";
        public static string BatchOperationIdLogPropertyName { get; set; } = "BatchOperationId";

        public static string FallbackCulture { get; set; } = "en-US";

        private static readonly Lazy<LogManager> Instance = new Lazy<LogManager>(() => new LogManager());

        private static readonly Lazy<ILoggerFactory> LazyLoggerFactory = new Lazy<ILoggerFactory>(() => new LoggerFactory());

        private readonly Dictionary<ILogPipelineProvider, ILogOptions> _extraPipelinesLogSettings =
            new Dictionary<ILogPipelineProvider, ILogOptions>(new LogPipelineComparer());

        private readonly Dictionary<string, LoggingLevelSwitch> _pipelineLevelSwitches =
            new Dictionary<string, LoggingLevelSwitch>();

        private readonly Dictionary<string, LoggingLevelSwitch> _overrideLevelSwitches =
            new Dictionary<string, LoggingLevelSwitch>();

        protected bool IsDevelopment => false;//TODO: KaiveSettings.IsDevelopment;

        public bool IsConfigured { get; private set; }

        public ILogOptions GlobalLogSettings { get; private set; }

        private void ConfigureLoggingPipelines()
        {
            // Root Logger Configuration:
            // The entrypoint for the logging pipeline. Must be always at a "Verbose" minimum level.
            // And then, all the subloggers will have its own minimum levels defined at a higher minimum.
            var rootLoggerConfiguration = CreateRootLoggerConfiguration();

            // Global Logging Pipeline:
            // This logger configuration is not related to any "AppEnvironment" variables.
            // The settings for this logger usually comes from the local settings (file + environment).
            // It can be used outside an multitenancy environment.
            ConfigurePipeline(rootLoggerConfiguration);

            // Extra Logging Pipelines:
            // for each "AppEnvironment" currently defined, we will have a specific logging pipeline.
            foreach (var pipelineProvider in _extraPipelinesLogSettings.Keys)
                ConfigurePipeline(pipelineProvider, rootLoggerConfiguration);

            // Overrides can only be defined at the root logger
            if (GlobalLogSettings.Overrides != null && GlobalLogSettings.Overrides.Count > 0)
                foreach (var sourceName in GlobalLogSettings.Overrides.Keys)
                {
                    var overrideMapKey = GetOverrideMapKey(sourceName, null);
                    var overridenLevelSwitch = _overrideLevelSwitches[overrideMapKey];
                    rootLoggerConfiguration.MinimumLevel.Override(sourceName, overridenLevelSwitch);
                }

            // Create the root logger, which contains all the logging pipelines currently defined.
            Log.Logger = rootLoggerConfiguration.CreateLogger();

            // Finally, when in dev-mode,
            // we will also enable the Serilog SelfLog streamming to the Console Output.
            if (IsDevelopment)
                SelfLog.Enable(Console.Out);
        }

        private void ConfigurePipeline(LoggerConfiguration rootPipelineConfiguration)
        {
            rootPipelineConfiguration
                .WriteTo.Logger(CreatePipelineLogger());
        }

        private void ConfigurePipeline(ILogPipelineProvider extraPipelineProvider, LoggerConfiguration rootPipelineConfiguration)
        {
            rootPipelineConfiguration
                .WriteTo.Logger(CreatePipelineLogger(extraPipelineProvider));
        }

        private LoggerConfiguration CreateRootLoggerConfiguration()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentVariables(EnvironmentVariablesToLog);
        }

        private Serilog.ILogger CreatePipelineLogger(ILogPipelineProvider extraPipelineProvider = null)
        {
            var logSettings = extraPipelineProvider == null
                ? GlobalLogSettings
                : _extraPipelinesLogSettings[extraPipelineProvider];

            var pipelineSwitchKey = GetLogLevelSwitchKey(extraPipelineProvider);
            var pipelineLevelSwitch = _pipelineLevelSwitches[pipelineSwitchKey];

            var pipelineConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(pipelineLevelSwitch)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentVariables(EnvironmentVariablesToLog);

            const string extraPipelineFilter = "IsExtraLogPipeline = true";
            if (extraPipelineProvider != null)
            {
                var pipelineProviderTypeName = extraPipelineProvider.GetType().Name;
                pipelineConfiguration
                    .Filter.ByIncludingOnly($"{extraPipelineFilter} and {pipelineProviderTypeName}.Id = '{extraPipelineProvider.Id}'");
            }
            else
            {
                pipelineConfiguration
                    .Filter.ByExcluding(extraPipelineFilter);
            }

            if (logSettings.MongoDb.IsEnabled)
            {
                const LogSinkType sinkType = LogSinkType.MongoDb;
                var switchKey = GetLogLevelSwitchKey(sinkType, extraPipelineProvider);
                var levelSwitch = _pipelineLevelSwitches[switchKey];

                pipelineConfiguration
                    .WriteTo.Logger(mongoDbLogConfiguration =>
                    {
                        mongoDbLogConfiguration
                            .MinimumLevel.ControlledBy(levelSwitch)
                            .Enrich.FromLogContext()
                            .Enrich.WithEnvironmentVariables(EnvironmentVariablesToLog);

                        mongoDbLogConfiguration
                            .WriteTo.MongoDBCapped(logSettings.MongoDb.ConnectionString, cappedMaxSizeMb: 512L,
                                collectionName: logSettings.MongoDb.CollectionName,
                                formatProvider: GetFormatProvider(logSettings.MongoDb.Culture ?? logSettings.Culture));
                    });
            }

            if (logSettings.Slack.IsEnabled)
            {
                const LogSinkType sinkType = LogSinkType.Slack;
                var settings = logSettings.Slack;
                var switchKey = GetLogLevelSwitchKey(sinkType, extraPipelineProvider);
                var levelSwitch = _pipelineLevelSwitches[switchKey];
                var slackOptions = new SlackSinkOptions
                {
                    WebHookUrl = settings.WebHookUrl,
                    CustomChannel = settings.CustomChannel,
                    CustomUserName = settings.CustomUsername,
                    CustomIcon = settings.CustomIcon,
                    BatchSizeLimit = settings.BatchSizeLimit,
                    Period = settings.Period
                };

                pipelineConfiguration
                    .WriteTo.Logger(slackLogConfiguration =>
                    {
                        slackLogConfiguration
                            .MinimumLevel.ControlledBy(levelSwitch)
                            .Enrich.FromLogContext()
                            .Enrich.WithEnvironmentVariables(EnvironmentVariablesToLog);

                        slackLogConfiguration
                            .WriteTo.Slack(slackOptions,
                                formatProvider: GetFormatProvider(logSettings.Slack.Culture ?? logSettings.Culture));
                    });
            }

            if (logSettings.Email.IsEnabled)
            {
                const LogSinkType sinkType = LogSinkType.Email;
                var settings = logSettings.Email;
                var switchKey = GetLogLevelSwitchKey(sinkType, extraPipelineProvider);
                var levelSwitch = _pipelineLevelSwitches[switchKey];
                var emailOptions = new EmailConnectionInfo
                {
                    EmailSubject = settings.Subject,
                    EnableSsl = settings.EnableSsl,
                    FromEmail = settings.From,
                    IsBodyHtml = settings.IsBodyHtml,
                    MailServer = settings.SmtpHost,
                    ToEmail = string.Join(";", settings.ToList)
                };
                emailOptions.Port = settings.SmtpPort ?? emailOptions.Port;
                if (!string.IsNullOrWhiteSpace(settings.SmtpUser))
                    emailOptions.NetworkCredentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPass);

                pipelineConfiguration
                    .WriteTo.Logger(emailLogConfiguration =>
                    {
                        emailLogConfiguration
                            .MinimumLevel.ControlledBy(levelSwitch)
                            .Enrich.FromLogContext()
                            .Enrich.WithEnvironmentVariables(EnvironmentVariablesToLog);

                        emailLogConfiguration
                            .WriteTo.Email(emailOptions, mailSubject: settings.Subject,
                                formatProvider: GetFormatProvider(logSettings.Email.Culture ?? logSettings.Culture));
                    });
            }
            
            if (logSettings.Seq.IsEnabled)
            {
                const LogSinkType sinkType = LogSinkType.Seq;
                var settings = logSettings.Seq;
                var switchKey = GetLogLevelSwitchKey(sinkType, extraPipelineProvider);
                var levelSwitch = _pipelineLevelSwitches[switchKey];

                pipelineConfiguration
                    .WriteTo.Logger(seqLogConfiguration =>
                    {
                        seqLogConfiguration
                            .MinimumLevel.ControlledBy(levelSwitch)
                            .Enrich.FromLogContext()
                            .Enrich.WithEnvironmentVariables(EnvironmentVariablesToLog);

                        seqLogConfiguration
                            .WriteTo.Seq(settings.Host, apiKey: settings.ApiKey);
                    });
            }

            if (logSettings.ElasticSearch.IsEnabled)
            {
                const LogSinkType sinkType = LogSinkType.ElasticSearch;
                var settings = logSettings.ElasticSearch;
                var switchKey = GetLogLevelSwitchKey(sinkType, extraPipelineProvider);
                var levelSwitch = _pipelineLevelSwitches[switchKey];

                pipelineConfiguration
                    .WriteTo.Logger(elasticSearchLogConfiguration =>
                    {
                        elasticSearchLogConfiguration
                            .MinimumLevel.ControlledBy(levelSwitch)
                            .Enrich.FromLogContext()
                            .Enrich.WithEnvironmentVariables(EnvironmentVariablesToLog);

                        elasticSearchLogConfiguration
                            .WriteTo.Elasticsearch(settings.Host, settings.IndexName);
                    });
            }

            // When in Development Mode,
            // Every and all logging will also be written to the trace and literate console, no matter what.
            // And since each logging pipeline excludes each other logging pipeline,
            // this should garantee that we will not have dupes into the Literate/Trace logging.
            if (IsDevelopment)
            {
                pipelineConfiguration
                    .WriteTo.Trace(formatProvider: GetFormatProvider(logSettings.Culture))
                    .WriteTo.Console(formatProvider: GetFormatProvider(logSettings.Culture));
            }

            return pipelineConfiguration.CreateLogger();
        }

        private static IFormatProvider GetFormatProvider(string culture)
        {
            culture = culture ?? FallbackCulture;
            if (string.IsNullOrWhiteSpace(FallbackCulture))
                return null;

            try
            {
                return new CultureInfo(culture);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Failed to create the requested culture info '{Culture}'.", culture, ex);
            }

            return null;
        }

        private static string GetLogLevelSwitchKey(ILogPipelineProvider extraPipelineProvider)
        {
            return GetLogLevelSwitchKey(LogSinkType.None, extraPipelineProvider);
        }

        private static string GetLogLevelSwitchKey(LogSinkType sinkType = LogSinkType.None,
            ILogPipelineProvider extraPipelineProvider = null)
        {
            var sinkTypeKey = $"{(sinkType == LogSinkType.None ? string.Empty : "::" + sinkType)}";
            return $"{extraPipelineProvider?.Id.ToString("N") ?? "Global"}{sinkTypeKey}";
        }

        private static string GetOverrideMapKey(string sourceName, ILogPipelineProvider extraPipelineProvider = null)
        {
            return GetOverrideMapKey(sourceName, LogSinkType.None, extraPipelineProvider);
        }

        private static string GetOverrideMapKey(string sourceName, LogSinkType sinkType = LogSinkType.None,
            ILogPipelineProvider extraPipelineProvider = null)
        {
            var pipelineKey = extraPipelineProvider?.Id.ToString("N") ?? "Global";
            var sinkTypeKey = $"{(sinkType == LogSinkType.None ? string.Empty : "::" + sinkType)}";
            return $"{pipelineKey}{sinkTypeKey}::{sourceName}";
        }

        private void SetGlobalPipelineSettings(ILogOptions logSettings)
        {
            GlobalLogSettings = logSettings;
            SetPipelineLevels(logSettings);
        }

        private void SetPipelineSettings(ILogPipelineProvider extraPipelineProvider)
        {
            var logSettings = extraPipelineProvider.Logging;
            _extraPipelinesLogSettings[extraPipelineProvider] = logSettings;
            SetPipelineLevels(logSettings, extraPipelineProvider);
        }

        private void SetPipelineLevels(ILogOptions logSettings, ILogPipelineProvider extraPipelineProvider = null)
        {
            var switchKey = GetLogLevelSwitchKey(extraPipelineProvider);
            var minLevel = logSettings.MinimumLevel;
            SetPipelineMinLevel(switchKey, minLevel);
            foreach (var sourceName in logSettings.Overrides.Keys)
            {
                var overrideKey = GetOverrideMapKey(sourceName, extraPipelineProvider);
                SetOverrideMinLevel(overrideKey, logSettings.Overrides[sourceName]);
            }

            switchKey = GetLogLevelSwitchKey(LogSinkType.MongoDb, extraPipelineProvider);
            minLevel = logSettings.MongoDb.MinimumLevel;
            SetPipelineMinLevel(switchKey, minLevel);

            switchKey = GetLogLevelSwitchKey(LogSinkType.Slack, extraPipelineProvider);
            minLevel = logSettings.Slack.MinimumLevel;
            SetPipelineMinLevel(switchKey, minLevel);

            switchKey = GetLogLevelSwitchKey(LogSinkType.Email, extraPipelineProvider);
            minLevel = logSettings.Email.MinimumLevel;
            SetPipelineMinLevel(switchKey, minLevel);
            
            switchKey = GetLogLevelSwitchKey(LogSinkType.Seq, extraPipelineProvider);
            minLevel = logSettings.Seq.MinimumLevel;
            SetPipelineMinLevel(switchKey, minLevel);

            switchKey = GetLogLevelSwitchKey(LogSinkType.ElasticSearch, extraPipelineProvider);
            minLevel = logSettings.ElasticSearch.MinimumLevel;
            SetPipelineMinLevel(switchKey, minLevel);
        }

        private ILogOptions GetPipelineSettings(ILogPipelineProvider extraPipelineProvider)
        {
            return _extraPipelinesLogSettings.TryGetValue(extraPipelineProvider, out var settings) ? settings : null;
        }

        private void SetPipelineMinLevel(string switchKey, LogLevel minLevel)
        {
            var keyExists = _pipelineLevelSwitches.ContainsKey(switchKey);
            if (keyExists)
                _pipelineLevelSwitches[switchKey].MinimumLevel = ConvertLevel(minLevel);
            else
                _pipelineLevelSwitches[switchKey] = new LoggingLevelSwitch(ConvertLevel(minLevel));
        }

        private void SetOverrideMinLevel(string switchKey, LogLevel minLevel)
        {
            var keyExists = _overrideLevelSwitches.ContainsKey(switchKey);
            if (keyExists)
                _overrideLevelSwitches[switchKey].MinimumLevel = ConvertLevel(minLevel);
            else
                _overrideLevelSwitches[switchKey] = new LoggingLevelSwitch(ConvertLevel(minLevel));
        }

        internal static LogEventLevel ConvertLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return LogEventLevel.Verbose;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
                case LogLevel.None:
                    return LogEventLevel.Fatal;
                default:
                    return LogEventLevel.Information;
            }
        }

        // ReSharper disable once UnusedMember.Global
        internal static LogLevel ConvertLevel(LogEventLevel logLevel)
        {
            switch (logLevel)
            {
                case LogEventLevel.Verbose:
                    return LogLevel.Trace;
                case LogEventLevel.Debug:
                    return LogLevel.Debug;
                case LogEventLevel.Information:
                    return LogLevel.Information;
                case LogEventLevel.Warning:
                    return LogLevel.Warning;
                case LogEventLevel.Error:
                    return LogLevel.Error;
                case LogEventLevel.Fatal:
                    return LogLevel.Critical;
                default:
                    return LogLevel.Information;
            }
        }

        private LogManager()
        {
        }

        public void Configure(ILogOptions globalLogSettings = null,
            IEnumerable<ILogPipelineProvider> extraPipelines = null)
        {
            if (IsConfigured)
                throw new InvalidOperationException("LogManager.Configure() cannot be called more than once.");

            IsConfigured = true;

            SetGlobalPipelineSettings(globalLogSettings ?? new LogSettings());
            if (extraPipelines != null)
                foreach (var pipelineProvider in extraPipelines)
                    SetPipelineSettings(pipelineProvider);

            ConfigureLoggingPipelines();

            LoggerFactory?.AddSerilog(dispose: true);
        }

        public void Reconfigure(ILogOptions newLogSettings = null)
        {
            if (!IsConfigured)
            {
                Configure(newLogSettings);
                return;
            }

            var oldLogSettings = GlobalLogSettings;
            newLogSettings = newLogSettings ?? new LogSettings();

            SetGlobalPipelineSettings(newLogSettings);

            // Check if the log settings have actually changed.
            if (new LogSettingsComparer().Equals(newLogSettings, oldLogSettings))
                return;

            CloseAndFlush();
            ConfigureLoggingPipelines();
        }

        public void Reconfigure(ILogPipelineProvider extraPipelineProvider)
        {
            Ensure.Argument.NotNull(extraPipelineProvider, nameof(extraPipelineProvider));

            Reconfigure(new[] { extraPipelineProvider });
        }

        public void Reconfigure(IEnumerable<ILogPipelineProvider> extraPipelines)
        {
            Ensure.Argument.NotNull(extraPipelines, nameof(extraPipelines));

            if (!IsConfigured)
            {
                Configure(GlobalLogSettings, extraPipelines);
                return;
            }

            var requiresPipelineRecreation = false;
            foreach (var appEnvironment in extraPipelines.Where(pipeline => pipeline != null))
            {
                var oldLogSettings = GetPipelineSettings(appEnvironment);
                var newLogSettings = appEnvironment.Logging;
                SetPipelineSettings(appEnvironment);

                // Check if the log settings have actually changed.
                if (!new LogSettingsComparer().Equals(newLogSettings, oldLogSettings))
                    requiresPipelineRecreation = true;
            }

            if (!requiresPipelineRecreation)
                return;

            CloseAndFlush();
            ConfigureLoggingPipelines();
        }

        public IDisposable SetLoggingContext<T>(T pipelineData)
        {
            if (!IsConfigured || pipelineData == null)
                return null;

            return Serilog.Context.LogContext.PushProperty(pipelineData.GetType().Name, pipelineData, true);
        }

        public void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }

        public void Debug(string messageTemplate, Dictionary<string, object> context, params object[] propertyValues)
        {
            var log = CreateLogContext(context);
            log.Debug(messageTemplate, propertyValues);
        }
        
        public void Warning(string messageTemplate, Dictionary<string, object> context, params object[] propertyValues)
        {
            var log = CreateLogContext(context);
            log.Warning(messageTemplate, propertyValues);
        }
        
        public void Information(string messageTemplate, Dictionary<string, object> context, params object[] propertyValues)
        {
            var log = CreateLogContext(context);
            log.Information(messageTemplate, propertyValues);
        }
        
        public void Error(string messageTemplate, Dictionary<string, object> context, params object[] propertyValues)
        {
            var log = CreateLogContext(context);
            log.Error(messageTemplate, propertyValues);
        }

        public void Error(Exception exception, string messageTemplate, Dictionary<string, object> context, params object[] propertyValues)
        {
            var log = CreateLogContext(context);
            log.Error(exception, messageTemplate, propertyValues);
        }
        
        private static Serilog.ILogger CreateLogContext(Dictionary<string, object> context)
        {
            return context.Aggregate(Log.Logger, (current, c) => current.ForContext(c.Key, c.Value));
        }

        public static ILoggerFactory LoggerFactory { get; private set; }

        public static LogManager CreateLoggerFactory(IServiceCollection services = null)
        {
            LoggerFactory = LazyLoggerFactory.Value;
            services?.TryAddSingleton(LoggerFactory);
            return Instance.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        // ReSharper disable once UnusedMember.Global
        public static void Setup(IConfigurationRoot configuration = null)
        {
            Instance.Value.Configure(configuration?.GetLogSettings());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logSettings"></param>
        // ReSharper disable once UnusedMember.Global
        public static void Setup(ILogOptions logSettings)
        {
            Instance.Value.Configure(logSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static LogManager GetInstance()
        {
            return Instance.Value;
        }
    }
}