using HttpBridge.Core;
using Microsoft.Extensions.DependencyInjection;
using HttpBridge.Flurl;

namespace HttpBridge.Tests;

public class GoogleFlurlClient(HttpClient httpClient) : FlurlClientBase(httpClient);

public class FlurlExtensionsTests
{
    [Fact]
    public async Task Registers_client_with_config_headers_policy_and_health()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddFlurlClient<GoogleFlurlClient, GoogleConfigurationOptions>(TestSupport.Config())
            .WithPolicy()
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<GoogleFlurlClient>());
        await TestSupport.VerifyHealthAsync(sp, handler);
    }

    [Fact]
    public async Task Authorization_handler_runs()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddFlurlClientWithAuthorization<GoogleFlurlClient, GoogleConfigurationOptions, AddAuthHeaderHandler>(
                TestSupport.Config())
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        await TestSupport.VerifyHealthAsync(sp, handler, expectAuth: true);
    }
}
