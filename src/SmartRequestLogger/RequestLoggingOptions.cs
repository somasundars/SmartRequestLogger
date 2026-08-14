namespace SmartRequestLogger;

/// <summary>
/// Configuration options for <see cref="RequestLoggingMiddleware"/>.
/// </summary>
public sealed class RequestLoggingOptions
{
    /// <summary>
    /// HTTP header name used to read/propagate the correlation ID.
    /// Defaults to "X-Correlation-ID".
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// When true, includes the request body in the log (subject to <see cref="MaxBodyLength"/>).
    /// Off by default to avoid accidentally logging sensitive payloads.
    /// </summary>
    public bool LogRequestBody { get; set; } = false;

    /// <summary>
    /// When true, includes the response body in the log (subject to <see cref="MaxBodyLength"/>).
    /// </summary>
    public bool LogResponseBody { get; set; } = false;

    /// <summary>
    /// Maximum number of characters of a request/response body to log before truncating.
    /// </summary>
    public int MaxBodyLength { get; set; } = 2000;

    /// <summary>
    /// Header names whose values should be redacted (replaced with "***") in logs.
    /// Matching is case-insensitive.
    /// </summary>
    public HashSet<string> RedactedHeaders { get; set; } =
        new(StringComparer.OrdinalIgnoreCase) { "Authorization", "Cookie", "Set-Cookie", "X-Api-Key" };

    /// <summary>
    /// Paths (or path prefixes) to skip logging for entirely, e.g. health checks.
    /// </summary>
    public HashSet<string> IgnorePaths { get; set; } =
        new(StringComparer.OrdinalIgnoreCase) { "/health", "/favicon.ico" };

    /// <summary>
    /// Minimum request duration in milliseconds before it's flagged as "slow" in the log
    /// (logged at Warning level instead of Information).
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 1000;
}
