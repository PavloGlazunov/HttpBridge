using HttpBridge.Core;
using HttpBridge.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBridge.Tests;

/// <summary>Bound from section "Google".</summary>
public class GoogleConfigurationOptions : BaseServiceConfigurationOptions;

/// <summary>Records requests, answers /health with Healthy and /health/deep with Unhealthy.</summary>
public sealed class StubHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request);
        string path = request.RequestUri!.AbsolutePath;
        if (path.Contains("/down/"))
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable) { Content = new StringContent("Unhealthy") });
        }

        string json = path.Contains("/pet/")
            ? """{"id":1,"name":"rex","status":"available"}"""
            : path.EndsWith("/deep")
            ? """{"status":"Unhealthy","errors":["db"]}"""
            : """{"status":"Healthy"}""";
        return Task.FromResult(new HttpResponseMessage { Content = new StringContent(json) });
    }
}

public sealed class AddAuthHeaderHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer test");
        return base.SendAsync(request, ct);
    }
}

public static class TestSupport
{
    public static IConfiguration Config(string section = "Google") => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            [$"{section}:BaseUrl"] = "https://google.test/api/v1",
            [$"{section}:Headers:function-key"] = "abc",
            [$"{section}:MaxRetry"] = "1",
        })
        .Build();

    /// <summary>Resolves the single registered <see cref="IHealthCheckableApiClient"/> and checks both probes end to end.</summary>
    public static async Task VerifyHealthAsync(ServiceProvider sp, StubHandler handler, bool expectAuth = false)
    {
        IHealthCheckableApiClient client = Assert.Single(sp.GetServices<IHealthCheckableApiClient>());

        Assert.Equal("Healthy", (await client.GetHealthAsync()).Status);
        var deep = await client.GetDeepHealthAsync("api");
        Assert.Equal("Unhealthy", deep.Status);
        Assert.Equal(["db"], deep.Errors);

        var down = await client.GetHealthAsync("down");
        Assert.NotNull(down);
        Assert.Equal("Unhealthy", down.Status);
        Assert.Contains("503", Assert.Single(down.Errors));

        Assert.Equal(
            ["/api/v1/health", "/api/v1/api/health/deep", "/api/v1/down/health"],
            handler.Requests.Select(r => r.RequestUri!.AbsolutePath).Distinct()); // 503 may be retried by .WithPolicy()
        Assert.All(handler.Requests, r =>
        {
            Assert.Equal("google.test", r.RequestUri!.Host);
            Assert.Equal("abc", Assert.Single(r.Headers.GetValues("function-key")));
            Assert.Equal(expectAuth, r.Headers.Contains("Authorization"));
        });
    }
}
