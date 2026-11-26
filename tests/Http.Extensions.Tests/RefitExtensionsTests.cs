using Http.Extensions.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Refit.Extensions;

namespace Http.Extensions.Tests;

public interface IGoogleRefitClient : IHealthCheckableRefitClient
{
    [Get("/search")]
    Task<string> SearchAsync();
}

public class RefitExtensionsTests
{
    [Fact]
    public async Task Registers_client_with_config_headers_policy_and_health()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddRefitClient<IGoogleRefitClient, GoogleConfigurationOptions>(TestSupport.Config())
            .WithPolicy()
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IGoogleRefitClient>());
        await TestSupport.VerifyHealthAsync(sp, handler);
    }

    [Fact]
    public async Task Authorization_handler_runs()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddRefitClientWithAuthorization<IGoogleRefitClient, GoogleConfigurationOptions, AddAuthHeaderHandler>(
                TestSupport.Config())
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        await TestSupport.VerifyHealthAsync(sp, handler, expectAuth: true);
    }

    [Fact]
    public void Headers_argument_is_used_when_config_has_none()
    {
        ServiceCollection services = new();
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["Google:BaseUrl"] = "https://google.test/" })
            .Build();

        var builder = services.AddRefitClient<IGoogleRefitClient, GoogleConfigurationOptions>(
            cfg, headers: new Dictionary<string, string> { ["function-key"] = "from-code" });

        Assert.Equal("from-code", builder.Options.Headers["function-key"]);
    }
}
