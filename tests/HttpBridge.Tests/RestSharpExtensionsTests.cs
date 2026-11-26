using HttpBridge.Core;
using Microsoft.Extensions.DependencyInjection;
using HttpBridge.RestSharp;

namespace HttpBridge.Tests;

public class GoogleRestSharpClient(HttpClient httpClient) : RestSharpClientBase(httpClient);

public class RestSharpExtensionsTests
{
    [Fact]
    public async Task Registers_client_with_config_headers_policy_and_health()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddRestSharpClient<GoogleRestSharpClient, GoogleConfigurationOptions>(TestSupport.Config())
            .WithPolicy()
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<GoogleRestSharpClient>());
        await TestSupport.VerifyHealthAsync(sp, handler);
    }

    [Fact]
    public async Task Authorization_handler_runs()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddRestSharpClientWithAuthorization<GoogleRestSharpClient, GoogleConfigurationOptions, AddAuthHeaderHandler>(
                TestSupport.Config())
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        await TestSupport.VerifyHealthAsync(sp, handler, expectAuth: true);
    }
}
