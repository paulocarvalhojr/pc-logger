using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace PC.Logger
{
    public partial class LogManager
    {
        private class ElasticSearchComparer : IEqualityComparer<LogSettingsBase.ElasticSearchSettings>
        {
            private readonly OverridesComparer _overridesComparer = new OverridesComparer();

            public bool Equals(LogSettingsBase.ElasticSearchSettings x, LogSettingsBase.ElasticSearchSettings y)
            {
                if (x == null && y == null)
                    return true;

                return x != null && y != null && x.IsEnabled == y.IsEnabled && x.Host.Equals(y.Host) &&
                       x.IndexName.Equals(y.IndexName);
            }

            public int GetHashCode(LogSettingsBase.ElasticSearchSettings obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}