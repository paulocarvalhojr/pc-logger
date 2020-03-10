using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace PC.Logger
{
    public partial class LogManager
    {
        private class EmailComparer : IEqualityComparer<LogSettingsBase.EmailSettings>
        {
            private readonly ListOfStringsComparer _toListComparer = new ListOfStringsComparer();
            private readonly OverridesComparer _overridesComparer = new OverridesComparer();

            public bool Equals(LogSettingsBase.EmailSettings x, LogSettingsBase.EmailSettings y)
            {
                if (x == null && y == null)
                    return true;

                return x != null && y != null && x.IsEnabled == y.IsEnabled && x.EnableSsl == y.EnableSsl &&
                       x.IsBodyHtml == y.IsBodyHtml && x.From == y.From && x.SmtpHost == y.SmtpHost &&
                       x.SmtpPort == y.SmtpPort && x.SmtpUser == y.SmtpUser && x.SmtpPass == y.SmtpPass &&
                       x.Subject == y.Subject && _toListComparer.Equals(x.ToList, y.ToList) &&
                       _overridesComparer.Equals(x.Overrides, y.Overrides);
            }

            public int GetHashCode(LogSettingsBase.EmailSettings obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}