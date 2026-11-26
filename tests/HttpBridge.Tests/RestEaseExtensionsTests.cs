using HttpBridge.Core;
using Microsoft.Extensions.DependencyInjection;
using RestEase;
using HttpBridge.RestEase;

namespace HttpBridge.Tests;

public interface IGoogleRestEaseClient : IHealthCheckableRestEaseClient
{
    [Get("search")]
    Task<string> SearchAsync();
}

public class RestEaseExtensionsTests
{
    [Fact]
    public async Task Registers_client_with_config_headers_policy_and_health()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddRestEaseClient<IGoogleRestEaseClient, GoogleConfigurationOptions>(TestSupport.Config())
            .WithPolicy()
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IGoogleRestEaseClient>());
        await TestSupport.VerifyHealthAsync(sp, handler);
    }

    [Fact]
    public async Task Authorization_handler_runs()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddRestEaseClientWithAuthorization<IGoogleRestEaseClient, GoogleConfigurationOptions, AddAuthHeaderHandler>(
                TestSupport.Config())
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        await TestSupport.VerifyHealthAsync(sp, handler, expectAuth: true);
    }
}
