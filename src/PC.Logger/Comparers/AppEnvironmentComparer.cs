using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace PC.Logger
{
    public partial class LogManager
    {
        private class LogPipelineComparer : IEqualityComparer<ILogPipelineProvider>
        {
            public bool Equals(ILogPipelineProvider x, ILogPipelineProvider y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(ILogPipelineProvider obj)
            {
                return obj.Id.GetHashCode();
            }
        }
    }
}