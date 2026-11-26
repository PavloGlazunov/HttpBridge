using Http.Extensions.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Http.Extensions.Core;

public sealed class ClientBuilder<TClient, TOptions>(IHttpClientBuilder builder, TOptions options)
    where TClient : class
    where TOptions : BaseServiceConfigurationOptions
{
    public IHttpClientBuilder HttpClientBuilder { get; } = builder;
    public TOptions Options { get; } = options;

    /// <summary>Adds retry + circuit breaker + timeouts from <typeparamref name="TOptions"/>. The configuration argument is kept for source compatibility.</summary>
    public ClientBuilder<TClient, TOptions> WithPolicy(IConfiguration configuration = null)
    {
        HttpClientBuilder.WithPolicy(Options);
        return this;
    }
}
