using RestSharp;
using System.Text.Json;
using RestSharp.Serializers.Json;
using HttpBridge.Core;
using HttpBridge.Core.Models;
using HttpBridge.Core.Utils;

namespace HttpBridge.RestSharp;

/// <summary>
/// Base class for typed RestSharp clients: wraps the factory-created <see cref="HttpClient"/> in a <see cref="RestClient"/>
/// that uses System.Text.Json (camelCase) and implements <c>health</c> / <c>health/deep</c>.
/// Subclasses need a constructor taking <see cref="HttpClient"/>.
/// </summary>
public abstract class RestSharpClientBase : IHealthCheckableApiClient
{
    protected RestSharpClientBase(HttpClient httpClient, JsonSerializerOptions jsonOptions = null)
    {
        jsonOptions ??= JsonSerializationSettingsBuilder.CreateDefaultJsonOptions();
        Client = new RestClient(httpClient, disposeHttpClient: false, configureSerialization: s => s.UseSystemTextJson(jsonOptions));
    }

    protected RestClient Client { get; }

    public Task<HealthCheckResult> GetHealthAsync(string prefix = null) => ReadHealthAsync(prefix, "health");

    public Task<HealthCheckResult> GetDeepHealthAsync(string prefix = null) => ReadHealthAsync(prefix, "health/deep");

    private Task<HealthCheckResult> ReadHealthAsync(string prefix, string path) =>
        ResponseHelper.ProbeAsync(async () =>
        {
            RestResponse response = await Client.ExecuteAsync(
                new RestRequest(string.IsNullOrEmpty(prefix) ? path : $"{prefix}/{path}"));
            return response.StatusCode != default
                ? ResponseHelper.ToHealthResult(response.StatusCode, response.Content)
                : HealthCheckResult.CreateUnhealthyResult([response.ErrorMessage ?? response.ResponseStatus.ToString()]);
        });
}
