using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace PC.Logger
{
    public partial class LogManager
    {
        private class MongoDbComparer : IEqualityComparer<LogSettingsBase.MongoDbSettings>
        {
            private readonly OverridesComparer _overridesComparer = new OverridesComparer();

            public bool Equals(LogSettingsBase.MongoDbSettings x, LogSettingsBase.MongoDbSettings y)
            {
                if (x == null && y == null)
                    return true;

                return x != null && y != null && x.IsEnabled == y.IsEnabled &&
                       x.ConnectionString == y.ConnectionString && x.CollectionName == y.CollectionName &&
                       _overridesComparer.Equals(x.Overrides, y.Overrides);
            }

            public int GetHashCode(LogSettingsBase.MongoDbSettings obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}