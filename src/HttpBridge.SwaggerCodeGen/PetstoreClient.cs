using System.Text.Json;
using HttpBridge.Core;
using HttpBridge.Core.Infrastructure;
using HttpBridge.Core.Models;
using HttpBridge.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBridge.SwaggerCodeGen.Petstore;

/// <summary>Adds System.Text.Json defaults and <c>health</c> endpoints to the generated client.</summary>
public partial class PetstoreClient : IHealthCheckableApiClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings) =>
        JsonSerializationSettingsBuilder.SetDefaultJsonOptions(settings);

    public Task<HealthCheckResult> GetHealthAsync(string prefix = null) => ReadHealthAsync(prefix, "health");

    public Task<HealthCheckResult> GetDeepHealthAsync(string prefix = null) => ReadHealthAsync(prefix, "health/deep");

    private Task<HealthCheckResult> ReadHealthAsync(string prefix, string path) =>
        ResponseHelper.ReadHealthAsync(_httpClient.GetAsync(string.IsNullOrEmpty(prefix) ? path : $"{prefix}/{path}"));
}

/// <summary>Bound from section "Petstore".</summary>
public class PetstoreConfigurationOptions : BaseServiceConfigurationOptions;

public static class PetstoreServiceCollectionExtensions
{
    /// <summary>Registers the generated client from the "Petstore" configuration section.</summary>
    public static ClientBuilder<IPetstoreClient, PetstoreConfigurationOptions> AddPetstoreClient(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddSwaggerClient<IPetstoreClient, PetstoreClient, PetstoreConfigurationOptions>(configuration);
}
