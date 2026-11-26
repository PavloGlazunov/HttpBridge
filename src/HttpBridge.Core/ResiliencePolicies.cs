using HttpBridge.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace HttpBridge.Core;

public static class ResiliencePolicies
{
    /// <summary>
    /// Retry (transient HTTP failures, timeouts, cancellation), per-attempt timeout, total timeout and circuit breaker
    /// from <paramref name="config"/>. The pipeline owns the timeouts, so <c>HttpClient.Timeout</c> is switched off.
    /// </summary>
    public static void WithPolicy(this IHttpClientBuilder builder, BaseServiceConfigurationOptions config)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(config.MaxRetry, 1, nameof(config.MaxRetry));
        ArgumentOutOfRangeException.ThrowIfLessThan(config.Timeout, 1, nameof(config.Timeout));

        // HttpClient.Timeout covers the whole pipeline, so it would cancel the attempt at the same moment as the
        // attempt timeout and no retry could ever run.
        builder.ConfigureHttpClient(client => client.Timeout = System.Threading.Timeout.InfiniteTimeSpan);

        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.ShouldHandle = args => ValueTask.FromResult(
                HttpClientResiliencePredicates.IsTransient(args.Outcome)
                || args.Outcome.Exception is OperationCanceledException);

            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.MaxRetryAttempts = config.MaxRetry;
            options.Retry.Delay = TimeSpan.FromSeconds(config.RetryDelay);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(config.Timeout);

            // first attempt + retries, plus headroom for the exponential back-off: delay * (2^retries - 1)
            double backoff = config.RetryDelay * (Math.Pow(2, config.MaxRetry) - 1);
            options.TotalRequestTimeout.Timeout =
                TimeSpan.FromSeconds(config.Timeout * (config.MaxRetry + 1) + backoff);

            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(config.Timeout * 3);
            options.CircuitBreaker.FailureRatio = config.CircuitBreakerFailureRatio;
            options.CircuitBreaker.MinimumThroughput = config.CircuitBreakerMinimumThroughput;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(config.CircuitBreakerBreakDuration);
        });
    }
}
