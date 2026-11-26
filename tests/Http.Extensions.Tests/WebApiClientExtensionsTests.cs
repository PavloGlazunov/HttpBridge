using Http.Extensions.Core;
using Microsoft.Extensions.DependencyInjection;
using WebApiClientCore.Attributes;
using WebApiClient.Extensions;

namespace Http.Extensions.Tests;

public interface IGoogleWebApiClient : IHealthCheckableWebApiClient
{
    [HttpGet("search")]
    Task<string> SearchAsync();
}

public class WebApiExtensionsTests
{
    [Fact]
    public async Task Registers_client_with_config_headers_policy_and_health()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddWebApiClient<IGoogleWebApiClient, GoogleConfigurationOptions>(TestSupport.Config())
            .WithPolicy()
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IGoogleWebApiClient>());
        await TestSupport.VerifyHealthAsync(sp, handler);
    }

    [Fact]
    public async Task Authorization_handler_runs()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddWebApiClientWithAuthorization<IGoogleWebApiClient, GoogleConfigurationOptions, AddAuthHeaderHandler>(
                TestSupport.Config())
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        await TestSupport.VerifyHealthAsync(sp, handler, expectAuth: true);
    }
}
