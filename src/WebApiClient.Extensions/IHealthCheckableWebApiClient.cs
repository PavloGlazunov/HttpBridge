using Http.Extensions.Core.Models;
using Http.Extensions.Core.Utils;
using WebApiClientCore.Attributes;

namespace WebApiClient.Extensions;

/// <summary>
/// Derive your WebApiClientCore interface from this to get <c>health</c> and <c>health/deep</c> (optionally prefixed).
/// The runtime proxy routes every interface member (default methods included) to the HTTP pipeline,
/// so <c>GetHealthAsync</c> / <c>GetDeepHealthAsync</c> are extension methods.
/// </summary>
public interface IHealthCheckableWebApiClient
{
    [HttpGet("health")]
    Task<HttpResponseMessage> GetHealth();

    [HttpGet("{prefix}/health")]
    Task<HttpResponseMessage> GetHealthWithPrefix(string prefix);

    [HttpGet("health/deep")]
    Task<HttpResponseMessage> GetDeepHealth();

    [HttpGet("{prefix}/health/deep")]
    Task<HttpResponseMessage> GetDeepHealthWithPrefix(string prefix);
}

public static class HealthCheckableWebApiClientExtensions
{
    public static Task<HealthCheckResult> GetHealthAsync(this IHealthCheckableWebApiClient client, string prefix = null) =>
        ResponseHelper.ReadHealthAsync(string.IsNullOrEmpty(prefix) ? client.GetHealth() : client.GetHealthWithPrefix(prefix));

    public static Task<HealthCheckResult> GetDeepHealthAsync(this IHealthCheckableWebApiClient client, string prefix = null) =>
        ResponseHelper.ReadHealthAsync(string.IsNullOrEmpty(prefix) ? client.GetDeepHealth() : client.GetDeepHealthWithPrefix(prefix));
}
