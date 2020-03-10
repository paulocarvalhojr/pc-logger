using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace PC.Logger
{
    public partial class LogManager
    {
        private class SlackComparer : IEqualityComparer<LogSettingsBase.SlackSettings>
        {
            private readonly OverridesComparer _overridesComparer = new OverridesComparer();

            public bool Equals(LogSettingsBase.SlackSettings x, LogSettingsBase.SlackSettings y)
            {
                if (x == null && y == null)
                    return true;

                return x != null && y != null && x.IsEnabled == y.IsEnabled && x.WebHookUrl == y.WebHookUrl &&
                       x.CustomChannel == y.CustomChannel && x.CustomUsername == y.CustomUsername &&
                       x.CustomIcon == y.CustomIcon && x.BatchSizeLimit == y.BatchSizeLimit && x.Period == y.Period &&
                       _overridesComparer.Equals(x.Overrides, y.Overrides);
            }

            public int GetHashCode(LogSettingsBase.SlackSettings obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}