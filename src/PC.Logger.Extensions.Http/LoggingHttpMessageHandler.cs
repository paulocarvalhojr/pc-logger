using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Core.Enrichers;

namespace PC.Logger.Extensions.Http
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public class LoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly string _typedClientName;

        private readonly ILogger _logger;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="typedClientName"></param>
        /// <param name="logger"></param>
        /// <param name="innerHandler"></param>
        public LoggingHttpMessageHandler(string typedClientName, ILogger logger, HttpMessageHandler innerHandler = null)
        {
            _typedClientName = typedClientName ?? throw new ArgumentNullException(nameof(typedClientName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (innerHandler != null)
                InnerHandler = innerHandler;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            HttpResponseMessage response;
            var stopwatch = ValueStopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("D");
            Log.RequestStart(_logger, _typedClientName, requestId, request);

            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (Exception exception)
            {
                await Log.RequestFailedAsync(_logger, _typedClientName, requestId, request, stopwatch.GetElapsedTime(), exception);
                throw;
            }

            await Log.RequestEndAsync(_logger, _typedClientName, requestId, request, stopwatch.GetElapsedTime(), response);

            return response;
        }

        // ReSharper disable once UnusedMember.Global
        public static LoggingHttpMessageHandler Create(string httpClientName, ILoggerFactory loggerFactory, HttpMessageHandler innerHandler = null)
        {
            return new LoggingHttpMessageHandler(httpClientName,
                loggerFactory.CreateLogger($"System.Net.Http.HttpClient.{httpClientName}.PrivateLoggingHandler"),
                innerHandler);
        }

        private static class Log
        {
            private static class Events
            {
                public static readonly EventId RequestStartEvent = new EventId(100, "RequestStart");
                public static readonly EventId RequestEndEvent = new EventId(101, "RequestEnd");
                public static readonly EventId RequestErrorEvent = new EventId(102, "RequestError");
            }

            private const string RequestMessageTemplate =
                "[{HttpClient}] ({MessageDirection}) HTTP request [{HttpMethod}] {Uri}";

            private static readonly Action<ILogger, string, string, HttpMethod, Uri, Exception> LogRequestStart =
                LoggerMessage.Define<string, string, HttpMethod, Uri>(
                    LogLevel.Debug,
                    Events.RequestStartEvent,
                    RequestMessageTemplate);

            private static readonly Action<ILogger, string, string, HttpMethod, Uri, double, Exception> LogRequestError =
                LoggerMessage.Define<string, string, HttpMethod, Uri, double>(
                    LogLevel.Error,
                    Events.RequestErrorEvent,
                    $"{RequestMessageTemplate} (took {{ElapsedMilliseconds}}ms - FAILED)");

            private static readonly Action<ILogger, string, string, HttpMethod, Uri, double, HttpStatusCode, Exception> LogRequestEnd =
                LoggerMessage.Define<string, string, HttpMethod, Uri, double, HttpStatusCode>(
                    LogLevel.Debug,
                    Events.RequestEndEvent,
                    $"{RequestMessageTemplate} (took {{ElapsedMilliseconds}}ms - {{StatusCode}})");

            public static void RequestStart(ILogger logger, string typedClientName, string requestId, HttpRequestMessage request)
            {
                using (LogContext.Push(
                    new PropertyEnricher("RequestId", requestId),
                    new PropertyEnricher("RequestType", request.GetType().Name)))
                {
                    LogRequestStart(logger, typedClientName, "Out", request.Method, request.RequestUri, null);
                }
            }

            public static async Task RequestFailedAsync(ILogger logger, string typedClientName, string requestId, HttpRequestMessage request, TimeSpan duration, Exception exception)
            {
                using (LogContext.Push(
                    new PropertyEnricher("RequestId", requestId),
                    new PropertyEnricher("RequestType", request.GetType().Name),
                    new PropertyEnricher("RequestHeaders", GetRequestHeaders(request), true),
                    new PropertyEnricher("RequestBody", await GetRequestBodyAsync(request)),
                    new PropertyEnricher("ResponseHeaders", string.Empty),
                    new PropertyEnricher("ResponseBody", string.Empty)))
                {
                    LogRequestError(logger, typedClientName, "Err", request.Method, request.RequestUri, duration.TotalMilliseconds, exception);
                }
            }

            public static async Task RequestEndAsync(ILogger logger, string typedClientName, string requestId, HttpRequestMessage request, TimeSpan duration, HttpResponseMessage response)
            {
                using (LogContext.Push(
                    new PropertyEnricher("RequestId", requestId),
                    new PropertyEnricher("RequestType", request.GetType().Name),
                    new PropertyEnricher("RequestHeaders", GetRequestHeaders(request), true),
                    new PropertyEnricher("RequestBody", await GetRequestBodyAsync(request)),
                    new PropertyEnricher("ResponseHeaders", GetResponseHeaders(response), true),
                    new PropertyEnricher("ResponseBody", await GetResponseBodyAsync(response))))
                {
                    LogRequestEnd(logger, typedClientName, "In", request.Method, request.RequestUri, duration.TotalMilliseconds, response.StatusCode, null);
                }
            }

            private static Dictionary<string, string> GetRequestHeaders(HttpRequestMessage request)
            {
                return new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Request, request?.Headers, request?.Content?.Headers).ToDictionary();
            }

            private static Dictionary<string, string> GetResponseHeaders(HttpResponseMessage response)
            {
                return new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Request, response?.Headers, response?.Content?.Headers).ToDictionary();
            }

            private static async Task<string> GetRequestBodyAsync(HttpRequestMessage request)
            {
                return request?.Content != null ? await request.Content.ReadAsStringAsync() : null;
            }

            private static async Task<string> GetResponseBodyAsync(HttpResponseMessage response)
            {
                return response?.Content != null ? await response.Content.ReadAsStringAsync() : null;
            }
        }
    }
}
