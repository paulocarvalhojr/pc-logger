namespace PC.Logger
{
    public class LogSettings : LogSettingsBase, ILogOptions
    {
        public const string LoggingSectionName = "Logging";

        public string ConfigSectionName => LoggingSectionName;

        public MongoDbSettings MongoDb { get; set; } = new MongoDbSettings();

        public SlackSettings Slack { get; set; } = new SlackSettings();

        public EmailSettings Email { get; set; } = new EmailSettings();
        
        public SeqSettings Seq { get; set; } = new SeqSettings();

        public ElasticSearchSettings ElasticSearch { get; set; } = new ElasticSearchSettings();
    }
}