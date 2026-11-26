using Http.Extensions.Core.Models;

namespace Http.Extensions.Core;

/// <summary>Library-agnostic health contract. Inject <c>IEnumerable&lt;IHealthCheckableApiClient&gt;</c> to probe all clients.</summary>
public interface IHealthCheckableApiClient
{
    Task<HealthCheckResult> GetHealthAsync(string prefix = null);

    Task<HealthCheckResult> GetDeepHealthAsync(string prefix = null);
}
