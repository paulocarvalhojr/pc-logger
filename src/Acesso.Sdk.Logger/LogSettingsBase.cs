using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PC.Logger
{
    public abstract class LogSettingsBase
    {
        public class MongoDbSettings : LogSettingsBase
        {
            public string ConnectionString { get; set; }

            public string CollectionName { get; set; }

            public bool IsEnabled { get; set; }

            public bool IsValid => !string.IsNullOrWhiteSpace(ConnectionString) &&
                                     !string.IsNullOrWhiteSpace(CollectionName);
        }

        public class SlackSettings : LogSettingsBase
        {
            public string WebHookUrl { get; set; }

            public string CustomChannel { get; set; }

            public string CustomUsername { get; set; }

            public string CustomIcon { get; set; }

            public int BatchSizeLimit { get; set; } = 1;

            public TimeSpan Period { get; set; } = new TimeSpan(0, 0, 1);

            public bool IsEnabled { get; set; }

            public bool IsValid => !string.IsNullOrWhiteSpace(WebHookUrl);
        }

        public class EmailSettings : LogSettingsBase
        {
            public string From { get; set; }

            public List<string> ToList { get; set; } = new List<string>();

            public string Subject { get; set; }

            public bool IsBodyHtml { get; set; }

            public bool EnableSsl { get; set; }

            public string SmtpHost { get; set; }

            public int? SmtpPort { get; set; }

            public string SmtpUser { get; set; }

            public string SmtpPass { get; set; }

            public bool IsEnabled { get; set; }

            public bool IsValid => !string.IsNullOrWhiteSpace(From) && ToList != null &&
                                     !ToList.All(string.IsNullOrWhiteSpace) &&
                                     !string.IsNullOrWhiteSpace(SmtpHost);
        }

        public class SeqSettings : LogSettingsBase
        {
            public string Host { get; set; }
            
            public string ApiKey { get; set; }
            
            public bool IsEnabled { get; set; }
            
            public bool IsValid => !string.IsNullOrWhiteSpace(Host) &&
                                   !string.IsNullOrWhiteSpace(ApiKey);
        }

        public class ElasticSearchSettings : LogSettingsBase
        {
            public string Host { get; set; }

            public string IndexName { get; set; }

            public bool IsEnabled { get; set; }

            public bool IsValid => !string.IsNullOrWhiteSpace(Host) &&
                                   !string.IsNullOrWhiteSpace(IndexName);
        }

        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        public Dictionary<string, LogLevel> Overrides { get; set; } = new Dictionary<string, LogLevel>();

        public string Culture { get; set; }
    }
}