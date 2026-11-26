using HttpBridge.Core;
using Microsoft.Extensions.DependencyInjection;
using HttpBridge.Refitter.Petstore;

namespace HttpBridge.Tests;

public class RefitterExtensionsTests
{
    [Fact]
    public async Task Generated_client_works_with_config_and_health()
    {
        StubHandler handler = new();
        ServiceCollection services = new();
        services.AddPetstoreClient(TestSupport.Config("Petstore"))
            .WithPolicy()
            .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider sp = services.BuildServiceProvider();

        Pet pet = await sp.GetRequiredService<IPetstoreApi>().GetPetById(1);
        Assert.Equal("rex", pet.Name);
        Assert.Equal("/pet/1", handler.Requests.Single().RequestUri!.AbsolutePath);

        handler.Requests.Clear();
        await TestSupport.VerifyHealthAsync(sp, handler);
    }
}
