using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace PC.Logger
{
    public interface ILogOptions
    {
        string Culture { get; set; }

        LogLevel MinimumLevel { get; set; }

        Dictionary<string, LogLevel> Overrides { get; set; }

        LogSettingsBase.MongoDbSettings MongoDb { get; set; }

        LogSettingsBase.SlackSettings Slack { get; set; }

        LogSettingsBase.EmailSettings Email { get; set; }
        
        LogSettingsBase.SeqSettings Seq { get; set; }

        LogSettingsBase.ElasticSearchSettings ElasticSearch { get; set; }
    }
}