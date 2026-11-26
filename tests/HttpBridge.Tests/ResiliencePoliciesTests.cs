using System.Net;
using HttpBridge.Core;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBridge.Tests;

public class ResiliencePoliciesTests
{
    private sealed class SequenceHandler(params Func<CancellationToken, Task<HttpStatusCode>>[] steps) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            new(await steps[Math.Min(Calls++, steps.Length - 1)](ct));
    }

    private static Func<CancellationToken, Task<HttpStatusCode>> Status(HttpStatusCode code) => _ => Task.FromResult(code);

    private static HttpClient Create(SequenceHandler handler, GoogleConfigurationOptions options, out ServiceProvider sp)
    {
        ServiceCollection services = new();
        IHttpClientBuilder builder = services.AddHttpClient("c").ConfigureDefaults(options);
        builder.WithPolicy(options);
        builder
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IHttpClientFactory>().CreateClient("c");
    }

    private static GoogleConfigurationOptions Options(int maxRetry = 2, int timeout = 5) =>
        new() { BaseUrl = "https://google.test/", MaxRetry = maxRetry, RetryDelay = 0, Timeout = timeout };

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    public async Task Transient_status_is_retried(HttpStatusCode first)
    {
        SequenceHandler handler = new(Status(first), Status(HttpStatusCode.OK));
        using HttpClient client = Create(handler, Options(), out ServiceProvider sp);
        using (sp)
        {
            HttpResponseMessage response = await client.GetAsync("x");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, handler.Calls);
        }
    }

    [Fact]
    public async Task Attempt_timeout_triggers_retry()
    {
        SequenceHandler handler = new(
            async ct =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return HttpStatusCode.OK;
            },
            Status(HttpStatusCode.OK));
        using HttpClient client = Create(handler, Options(maxRetry: 1, timeout: 1), out ServiceProvider sp);
        using (sp)
        {
            HttpResponseMessage response = await client.GetAsync("x");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, handler.Calls);
        }
    }

    [Fact]
    public void HttpClient_timeout_is_left_to_the_pipeline()
    {
        using HttpClient client = Create(new SequenceHandler(Status(HttpStatusCode.OK)), Options(), out ServiceProvider sp);
        using (sp)
        {
            Assert.Equal(Timeout.InfiniteTimeSpan, client.Timeout);
        }
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(3, 0)]
    public void Invalid_options_are_rejected(int maxRetry, int timeout)
    {
        ServiceCollection services = new();
        GoogleConfigurationOptions options = Options(maxRetry, timeout);

        Assert.Throws<ArgumentOutOfRangeException>(() => services.AddHttpClient("c").WithPolicy(options));
    }
}
