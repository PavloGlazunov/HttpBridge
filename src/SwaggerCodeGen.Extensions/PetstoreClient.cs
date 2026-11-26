using System.Text.Json;
using Http.Extensions.Core;
using Http.Extensions.Core.Infrastructure;
using Http.Extensions.Core.Models;
using Http.Extensions.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SwaggerCodeGen.Extensions.Petstore;

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
