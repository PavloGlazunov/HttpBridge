using System.Text.Json;
using Flurl.Http;
using Flurl.Http.Configuration;
using Http.Extensions.Core;
using Http.Extensions.Core.Models;
using Http.Extensions.Core.Utils;

namespace Flurl.Http.Extensions;

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

    private async Task<HealthCheckResult> ReadHealthAsync(string prefix, params string[] segments)
    {
        IEnumerable<string> path = string.IsNullOrEmpty(prefix) ? segments : segments.Prepend(prefix);
        IFlurlResponse response = await Client.Request(path.ToArray()).AllowAnyHttpStatus().GetAsync();
        return await ResponseHelper.ReadHealthAsync(Task.FromResult(response.ResponseMessage));
    }
}
