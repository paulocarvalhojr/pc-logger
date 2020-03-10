using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Serilog.Core;

namespace PC.Logger.Enrichers.HttpContext
{
    public class LogContextMiddlewareOptions : IOptions<LogContextMiddlewareOptions>
    {
        public LogContextMiddlewareOptions Value => this;

        public Func<Microsoft.AspNetCore.Http.HttpContext, IEnumerable<ILogEventEnricher>> EnrichersForContextFactory { get; set; }

        public bool ReThrowEnricherFactoryExceptions { get; set; } = true;
    }
}