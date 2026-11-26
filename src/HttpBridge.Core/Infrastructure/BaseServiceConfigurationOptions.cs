namespace HttpBridge.Core.Infrastructure;

public abstract class BaseServiceConfigurationOptions
{
    public string BaseUrl { get; set; }
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public int MaxRetry { get; set; } = 3;
    public int RetryDelay { get; set; } = 3;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
    public int CircuitBreakerBreakDuration { get; set; } = 30;
    public int Timeout { get; set; } = 60;
}
