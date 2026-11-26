using HttpBridge.Core;
using HttpBridge.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HttpBridge.Refit;

namespace HttpBridge.Refitter.Petstore;

/// <summary>Adds <c>/health</c> endpoints to the generated client.</summary>
public partial interface IPetstoreApi : IHealthCheckableRefitClient;

/// <summary>Bound from section "Petstore".</summary>
public class PetstoreConfigurationOptions : BaseServiceConfigurationOptions;

public static class PetstoreServiceCollectionExtensions
{
    /// <summary>Registers the generated client from the "Petstore" configuration section.</summary>
    public static ClientBuilder<IPetstoreApi, PetstoreConfigurationOptions> AddPetstoreClient(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddRefitClient<IPetstoreApi, PetstoreConfigurationOptions>(configuration);
}
