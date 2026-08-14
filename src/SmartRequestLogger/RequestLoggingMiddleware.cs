using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartRequestLogger;

/// <summary>
/// Middleware that logs structured request/response information: method, path, status code,
/// duration, and a correlation ID that is generated (or read from an incoming header) and
/// propagated to the response and to the logging scope for the lifetime of the request.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly RequestLoggingOptions _options;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IOptions<RequestLoggingOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var correlationId = GetOrCreateCorrelationId(context);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_options.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        var stopwatch = Stopwatch.StartNew();
        string? requestBody = null;

        if (_options.LogRequestBody && context.Request.ContentLength > 0)
        {
            requestBody = await ReadRequestBodyAsync(context.Request);
        }

        // Capture the response body only if configured to, since it requires swapping the stream.
        Stream? originalResponseBody = null;
        MemoryStream? responseBuffer = null;

        if (_options.LogResponseBody)
        {
            originalResponseBody = context.Response.Body;
            responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            string? responseBody = null;
            if (_options.LogResponseBody && responseBuffer is not null && originalResponseBody is not null)
            {
                responseBuffer.Position = 0;
                responseBody = await new StreamReader(responseBuffer).ReadToEndAsync();
                responseBuffer.Position = 0;
                await responseBuffer.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;
            }

            LogRequest(context, stopwatch.ElapsedMilliseconds, requestBody, responseBody);
        }
    }

    private void LogRequest(HttpContext context, long elapsedMs, string? requestBody, string? responseBody)
    {
        var level = elapsedMs >= _options.SlowRequestThresholdMs ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(
            level,
            "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms{SlowFlag}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsedMs,
            elapsedMs >= _options.SlowRequestThresholdMs ? " [SLOW]" : "");

        if (requestBody is not null)
        {
            _logger.LogDebug("Request body: {RequestBody}", Truncate(requestBody));
        }

        if (responseBody is not null)
        {
            _logger.LogDebug("Response body: {ResponseBody}", Truncate(responseBody));
        }
    }

    private string Truncate(string value) =>
        value.Length <= _options.MaxBodyLength
            ? value
            : value[.._options.MaxBodyLength] + "... [truncated]";

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out var existing) &&
            !string.IsNullOrWhiteSpace(existing))
        {
            return existing!;
        }

        return Guid.NewGuid().ToString("N");
    }

    private bool ShouldSkip(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value)) return false;

        foreach (var ignored in _options.IgnorePaths)
        {
            if (value.StartsWith(ignored, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
