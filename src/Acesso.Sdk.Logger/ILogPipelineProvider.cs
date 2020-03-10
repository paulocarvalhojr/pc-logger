using System;

namespace PC.Logger
{
    public interface ILogPipelineProvider
    {
        Guid Id { get; }

        ILogOptions Logging { get; }
    }
}