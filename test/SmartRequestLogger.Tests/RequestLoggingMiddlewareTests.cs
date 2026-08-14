using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SmartRequestLogger.Tests;

public class RequestLoggingMiddlewareTests
{
    private static TestServer CreateServer(Action<RequestLoggingOptions>? configure = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddSmartRequestLogger(configure);
            })
            .Configure(app =>
            {
                app.UseSmartRequestLogger();
                app.Run(async context =>
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("ok");
                });
            });

        return new TestServer(builder);
    }

    [Fact]
    public async Task AddsCorrelationIdHeader_WhenNotProvidedByClient()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();

        var response = await client.GetAsync("/anything");

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.NotEmpty(response.Headers.GetValues("X-Correlation-ID").First());
    }

    [Fact]
    public async Task PropagatesClientSuppliedCorrelationId()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/anything");
        request.Headers.Add("X-Correlation-ID", "test-correlation-123");

        var response = await client.SendAsync(request);

        Assert.Equal("test-correlation-123", response.Headers.GetValues("X-Correlation-ID").First());
    }

    [Fact]
    public async Task SkipsIgnoredPaths()
    {
        using var server = CreateServer(opts => opts.IgnorePaths.Add("/health"));
        using var client = server.CreateClient();

        var response = await client.GetAsync("/health");

        // Middleware should pass through without adding a correlation header since it's skipped.
        Assert.False(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task RespectsCustomCorrelationHeaderName()
    {
        using var server = CreateServer(opts => opts.CorrelationIdHeader = "X-Trace-Id");
        using var client = server.CreateClient();

        var response = await client.GetAsync("/anything");

        Assert.True(response.Headers.Contains("X-Trace-Id"));
    }
}
