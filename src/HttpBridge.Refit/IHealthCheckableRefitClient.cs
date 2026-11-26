using Refit;
using HttpBridge.Core.Models;
using HttpBridge.Core.Utils;

namespace HttpBridge.Refit;

/// <summary>
/// Derive your Refit interface from this to get <c>/health</c> and <c>/health/deep</c> (optionally prefixed).
/// Intentionally does not inherit <c>IHealthCheckableApiClient</c>: Refit would generate throwing stubs for inherited members.
/// </summary>
public interface IHealthCheckableRefitClient
{
    Task<HealthCheckResult> GetHealthAsync(string prefix = null) =>
        ResponseHelper.ReadHealthAsync(string.IsNullOrEmpty(prefix) ? GetHealth() : GetHealthWithPrefix(prefix));

    Task<HealthCheckResult> GetDeepHealthAsync(string prefix = null) =>
        ResponseHelper.ReadHealthAsync(string.IsNullOrEmpty(prefix) ? GetDeepHealth() : GetDeepHealthWithPrefix(prefix));

    [Get("/health")]
    Task<HttpResponseMessage> GetHealth();

    [Get("/{prefix}/health")]
    Task<HttpResponseMessage> GetHealthWithPrefix([AliasAs("prefix")] string prefix);

    [Get("/health/deep")]
    Task<HttpResponseMessage> GetDeepHealth();

    [Get("/{prefix}/health/deep")]
    Task<HttpResponseMessage> GetDeepHealthWithPrefix([AliasAs("prefix")] string prefix);
}
