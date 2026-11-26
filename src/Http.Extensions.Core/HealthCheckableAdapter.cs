using Http.Extensions.Core.Models;

namespace Http.Extensions.Core;

/// <summary>Exposes a client whose proxy cannot implement <see cref="IHealthCheckableApiClient"/> directly (Refit, RestEase, ...).</summary>
public sealed class HealthCheckableAdapter(
    Func<string, Task<HealthCheckResult>> health,
    Func<string, Task<HealthCheckResult>> deepHealth) : IHealthCheckableApiClient
{
    public Task<HealthCheckResult> GetHealthAsync(string prefix = null) => health(prefix);

    public Task<HealthCheckResult> GetDeepHealthAsync(string prefix = null) => deepHealth(prefix);
}
