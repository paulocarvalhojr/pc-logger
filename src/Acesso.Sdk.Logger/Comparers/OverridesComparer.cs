using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace PC.Logger
{
    public partial class LogManager
    {
        private class OverridesComparer : IEqualityComparer<Dictionary<string, LogLevel>>
        {
            private readonly bool _compareValues;
            private readonly IEqualityComparer<LogLevel> _logLevelsComparer;
            public OverridesComparer(IEqualityComparer<LogLevel> logLevelsComparer = null, bool compareValues = false)
            {
                _compareValues = compareValues;
                _logLevelsComparer = logLevelsComparer;
            }

            public bool Equals(Dictionary<string, LogLevel> x, Dictionary<string, LogLevel> y)
            {
                if (x == null && y == null)
                    return true;
                if (x?.Count != y?.Count)
                    return false;
                if (x.Keys.Except(y.Keys).Any())
                    return false;
                if (y.Keys.Except(x.Keys).Any())
                    return false;

                // All keys matched. If we don't need to match the values, return true.
                if (!_compareValues)
                    return true;

                // We must compare the values.
                // Let's match the values from each dictionary now.
                return !(from pair in x
                    let valX = pair.Value
                    let valY = y[pair.Key]
                    where _logLevelsComparer != null && !_logLevelsComparer.Equals(valX, valY) || valX != valY
                    select valX).Any();
            }

            public int GetHashCode(Dictionary<string, LogLevel> obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}