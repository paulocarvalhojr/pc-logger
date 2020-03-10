using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace PC.Logger
{
    public partial class LogManager
    {
        private class SeqComparer : IEqualityComparer<LogSettingsBase.SeqSettings>
        {
            private readonly OverridesComparer _overridesComparer = new OverridesComparer();

            public bool Equals(LogSettingsBase.SeqSettings x, LogSettingsBase.SeqSettings y)
            {
                if (x == null && y == null)
                    return true;

                return x != null && y != null && x.IsEnabled == y.IsEnabled && x.ApiKey.Equals(y.ApiKey) &&
                       x.Host.Equals(y.Host) && _overridesComparer.Equals(x.Overrides, y.Overrides);
            }

            public int GetHashCode(LogSettingsBase.SeqSettings obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}