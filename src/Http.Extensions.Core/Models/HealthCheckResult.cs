using System.Text.Json.Serialization;

namespace Http.Extensions.Core.Models;

public class HealthCheckResult
{
    public const string HealthyStatus = "Healthy";
    public const string UnhealthyStatus = "Unhealthy";

    [JsonConstructor]
    private HealthCheckResult()
    {
        Status = HealthyStatus;
    }

    public static HealthCheckResult Healthy { get; } = new();

    public string Status { get; init; }

    public ICollection<string> Errors { get; init; }

    public static HealthCheckResult CreateUnhealthyResult(IEnumerable<string> errors) =>
        new()
        {
            Status = UnhealthyStatus,
            Errors = errors as ICollection<string> ?? errors.ToArray(),
        };
}
