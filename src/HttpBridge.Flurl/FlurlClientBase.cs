using System.Text.Json;
using Flurl.Http;
using Flurl.Http.Configuration;
using HttpBridge.Core;
using HttpBridge.Core.Models;
using HttpBridge.Core.Utils;

namespace HttpBridge.Flurl;

/// <summary>
/// Base class for typed Flurl clients: wraps the factory-created <see cref="HttpClient"/> in a <see cref="FlurlClient"/>
/// that uses System.Text.Json (camelCase) and implements <c>health</c> / <c>health/deep</c>.
/// Subclasses need a constructor taking <see cref="HttpClient"/>.
/// </summary>
public abstract class FlurlClientBase : IHealthCheckableApiClient
{
    protected FlurlClientBase(HttpClient httpClient, JsonSerializerOptions jsonOptions = null)
    {
        Client = new FlurlClient(httpClient);
        Client.Settings.JsonSerializer =
            new DefaultJsonSerializer(jsonOptions ?? JsonSerializationSettingsBuilder.CreateDefaultJsonOptions());
    }

    protected IFlurlClient Client { get; }

    public Task<HealthCheckResult> GetHealthAsync(string prefix = null) => ReadHealthAsync(prefix, "health");

    public Task<HealthCheckResult> GetDeepHealthAsync(string prefix = null) => ReadHealthAsync(prefix, "health", "deep");

    private Task<HealthCheckResult> ReadHealthAsync(string prefix, params string[] segments)
    {
        IEnumerable<string> path = string.IsNullOrEmpty(prefix) ? segments : segments.Prepend(prefix);

        async Task<HttpResponseMessage> SendAsync() =>
            (await Client.Request(path.ToArray()).AllowAnyHttpStatus().GetAsync()).ResponseMessage;

        return ResponseHelper.ReadHealthAsync(SendAsync());
    }
}
