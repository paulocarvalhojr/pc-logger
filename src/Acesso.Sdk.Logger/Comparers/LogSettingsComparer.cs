using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace PC.Logger
{
    public partial class LogManager
    {
        private class LogSettingsComparer : IEqualityComparer<ILogOptions>
        {
            private readonly OverridesComparer _overridesComparer = new OverridesComparer();
            private readonly MongoDbComparer _mongoDbComparer = new MongoDbComparer();
            private readonly SlackComparer _slackComparer = new SlackComparer();
            private readonly EmailComparer _emailComparer = new EmailComparer();
            private readonly SeqComparer _seqComparer = new SeqComparer();
            private readonly ElasticSearchComparer _elasticSearchComparer = new ElasticSearchComparer();

            public bool Equals(ILogOptions x, ILogOptions y)
            {
                if (x == null && y == null)
                    return true;

                return x != null && y != null && _overridesComparer.Equals(x.Overrides, y.Overrides)
                       && _mongoDbComparer.Equals(x.MongoDb, y.MongoDb) 
                       && _slackComparer.Equals(x.Slack, y.Slack)
                       && _seqComparer.Equals(x.Seq, y.Seq)
                       && _emailComparer.Equals(x.Email, y.Email)
                       && _elasticSearchComparer.Equals(x.ElasticSearch, y.ElasticSearch);
            }

            public int GetHashCode(ILogOptions obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}