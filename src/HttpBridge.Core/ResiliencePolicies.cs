using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Timeout;
using HttpBridge.Core.Infrastructure;

namespace HttpBridge.Core;

public static class ResiliencePolicies
{
    public static void WithPolicy(this IHttpClientBuilder builder, BaseServiceConfigurationOptions config)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.ShouldHandle = args =>
                ValueTask.FromResult(
                    args.Outcome.Exception is OperationCanceledException
                        or HttpRequestException
                        or TimeoutRejectedException);

            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.MaxRetryAttempts = config.MaxRetry;
            options.Retry.Delay = TimeSpan.FromSeconds(config.RetryDelay);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(config.Timeout * config.MaxRetry);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(config.Timeout);

            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(config.Timeout * 3);
            options.CircuitBreaker.FailureRatio = config.CircuitBreakerFailureRatio;
            options.CircuitBreaker.MinimumThroughput = config.CircuitBreakerMinimumThroughput;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(config.CircuitBreakerBreakDuration);
        });
    }
}
