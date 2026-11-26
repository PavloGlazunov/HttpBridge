using System.Net;
using System.Text.Json;
using RestSharp.Serializers.Json;
using Http.Extensions.Core;
using Http.Extensions.Core.Models;
using Http.Extensions.Core.Utils;

namespace RestSharp.Extensions;

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

    private async Task<HealthCheckResult> ReadHealthAsync(string prefix, string path)
    {
        RestResponse<HealthCheckResult> response = await Client.ExecuteAsync<HealthCheckResult>(
            new RestRequest(string.IsNullOrEmpty(prefix) ? path : $"{prefix}/{path}"));
        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest ? response.Data : null;
    }
}
