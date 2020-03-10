using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace PC.Logger
{
    public partial class LogManager
    {
        private class ListOfStringsComparer : IEqualityComparer<List<string>>
        {
            public bool Equals(List<string> x, List<string> y)
            {
                return x == null && y == null
                       || x != null && y != null && x.Count == y.Count && !x.Except(y).Union(y.Except(x)).Any();
            }

            public int GetHashCode(List<string> obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}