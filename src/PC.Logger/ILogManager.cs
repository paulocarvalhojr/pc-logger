using System;
using System.Collections.Generic;

namespace PC.Logger
{
    public interface ILogManager
    {
        bool IsConfigured { get; }

        ILogOptions GlobalLogSettings { get; }

        void Configure(ILogOptions globalLogSettings,
            IEnumerable<ILogPipelineProvider> extraPipelines = null);

        void Reconfigure(ILogOptions newLogSettings = null);

        void Reconfigure(ILogPipelineProvider extraPipelineProvider);

        void Reconfigure(IEnumerable<ILogPipelineProvider> extraPipelines);

        IDisposable SetLoggingContext<T>(T pipelineData);

        void CloseAndFlush();

        void Debug(string messageTemplate, Dictionary<string, object> context, params object[] propertyValues);
        
        void Warning(string messageTemplate, Dictionary<string, object> context, params object[] propertyValues);
        
        void Information(string messageTemplate, Dictionary<string, object> context, params object[] propertyValues);

        void Error(string messageTemplate, Dictionary<string, object> context, params object[] propertyValues);

        void Error(Exception exception, string messageTemplate, Dictionary<string, object> context, params object[] propertyValues);
    }
}