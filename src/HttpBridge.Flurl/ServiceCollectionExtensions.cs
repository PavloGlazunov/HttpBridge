using HttpBridge.Core;
using HttpBridge.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBridge.Flurl;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers <typeparamref name="TClient"/> as a typed HttpClient (constructor receives the configured HttpClient).</summary>
    public static ClientBuilder<TClient, TOptions> AddFlurlClient<TClient, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string httpClientName = null,
        IReadOnlyDictionary<string, string> headers = null)
        where TClient : FlurlClientBase
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        return Register<TClient, TOptions>(services, configuration, httpClientName, headers, null);
    }

    public static ClientBuilder<TClient, TOptions> AddFlurlClientWithAuthorization<TClient, TOptions, TAuthHeaderHandler>(
        this IServiceCollection services,
        IConfiguration configuration,
        string httpClientName = null,
        IReadOnlyDictionary<string, string> headers = null)
        where TClient : FlurlClientBase
        where TOptions : BaseServiceConfigurationOptions, new()
        where TAuthHeaderHandler : DelegatingHandler
    {
        return Register<TClient, TOptions>(
            services,
            configuration,
            httpClientName,
            headers,
            b => b.AddAuthorizationHandler<TAuthHeaderHandler>());
    }

    private static ClientBuilder<TClient, TOptions> Register<TClient, TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        string httpClientName,
        IReadOnlyDictionary<string, string> headers,
        Action<IHttpClientBuilder> addHandler)
        where TClient : FlurlClientBase
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        TOptions options = services.BindOptions<TOptions>(configuration, headers);
        IHttpClientBuilder builder = services
            .AddHttpClient<TClient>(httpClientName ?? typeof(TClient).Name)
            .ConfigureDefaults(options);
        addHandler?.Invoke(builder);
        services.AddHealthCheckable<TClient>();

        return new ClientBuilder<TClient, TOptions>(builder, options);
    }
}
