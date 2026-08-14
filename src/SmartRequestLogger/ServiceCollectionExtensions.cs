using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SmartRequestLogger;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="RequestLoggingOptions"/> with default values, optionally customized.
    /// Call this in Program.cs before building the app.
    /// </summary>
    public static IServiceCollection AddSmartRequestLogger(
        this IServiceCollection services,
        Action<RequestLoggingOptions>? configure = null)
    {
        var options = new RequestLoggingOptions();
        configure?.Invoke(options);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));
        return services;
    }
}

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="RequestLoggingMiddleware"/> to the pipeline. Place this early,
    /// right after exception-handling middleware, so it captures the full request lifecycle.
    /// </summary>
    public static IApplicationBuilder UseSmartRequestLogger(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
