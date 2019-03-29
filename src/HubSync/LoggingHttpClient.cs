using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;

namespace HubSync
{
    internal class LoggingHttpClient : IHttpClient
    {
        private readonly ILogger<LoggingHttpClient> _logger;
        private readonly HttpClientAdapter _innerClient;

        public LoggingHttpClient(ILogger<LoggingHttpClient> logger)
        {
            _logger = logger;

            // Create the default client
            _innerClient = new HttpClientAdapter(HttpMessageHandlerFactory.CreateDefault);
        }

        public void Dispose()
        {
            _logger.LogDebug("Disposing http client");
            _innerClient.Dispose();
        }

        public async Task<IResponse> Send(IRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("> {Method} {Url} ({ContentType})", request.Method, request.Endpoint, request.ContentType);
            var resp = await _innerClient.Send(request, cancellationToken);
            _logger.LogDebug("< {StatusCode} {ContentType} ({RateLimitRemaining} rate limit remaining, resets at {RateLimitReset:O})",
                resp.StatusCode, resp.ContentType, resp.ApiInfo.RateLimit.Remaining, resp.ApiInfo.RateLimit.Reset.LocalDateTime);
            return resp;
        }

        public void SetRequestTimeout(TimeSpan timeout)
        {
            _logger.LogDebug("Set response timeout to {Timeout}", timeout);
            _innerClient.SetRequestTimeout(timeout);
        }
    }
}