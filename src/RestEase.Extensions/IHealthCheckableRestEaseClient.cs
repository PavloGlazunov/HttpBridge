using Http.Extensions.Core.Models;
using Http.Extensions.Core.Utils;

namespace RestEase.Extensions;

/// <summary>
/// Derive your RestEase interface from this to get <c>health</c> and <c>health/deep</c> (optionally prefixed).
/// RestEase rejects default interface methods, so <c>GetHealthAsync</c> / <c>GetDeepHealthAsync</c> are extension methods.
/// </summary>
public interface IHealthCheckableRestEaseClient
{
    [Get("health")]
    Task<HttpResponseMessage> GetHealth();

    [Get("{prefix}/health")]
    Task<HttpResponseMessage> GetHealthWithPrefix([Path("prefix")] string prefix);

    [Get("health/deep")]
    Task<HttpResponseMessage> GetDeepHealth();

    [Get("{prefix}/health/deep")]
    Task<HttpResponseMessage> GetDeepHealthWithPrefix([Path("prefix")] string prefix);
}

public static class HealthCheckableRestEaseClientExtensions
{
    public static Task<HealthCheckResult> GetHealthAsync(this IHealthCheckableRestEaseClient client, string prefix = null) =>
        ResponseHelper.ReadHealthAsync(string.IsNullOrEmpty(prefix) ? client.GetHealth() : client.GetHealthWithPrefix(prefix));

    public static Task<HealthCheckResult> GetDeepHealthAsync(this IHealthCheckableRestEaseClient client, string prefix = null) =>
        ResponseHelper.ReadHealthAsync(string.IsNullOrEmpty(prefix) ? client.GetDeepHealth() : client.GetDeepHealthWithPrefix(prefix));
}
