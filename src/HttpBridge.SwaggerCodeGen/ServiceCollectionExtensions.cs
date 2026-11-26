using HttpBridge.Core;
using HttpBridge.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBridge.SwaggerCodeGen;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an NSwag-generated client (<typeparamref name="TClient"/> with a constructor taking <see cref="HttpClient"/>)
    /// behind <typeparamref name="TInterface"/>. If the client implements <see cref="IHealthCheckableApiClient"/> it is also
    /// registered as such.
    /// </summary>
    public static ClientBuilder<TInterface, TOptions> AddSwaggerClient<TInterface, TClient, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string httpClientName = null,
        IReadOnlyDictionary<string, string> headers = null)
        where TInterface : class
        where TClient : class, TInterface
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        return Register<TInterface, TClient, TOptions>(services, configuration, httpClientName, headers, null);
    }

    public static ClientBuilder<TInterface, TOptions> AddSwaggerClientWithAuthorization<TInterface, TClient, TOptions,
        TAuthHeaderHandler>(
        this IServiceCollection services,
        IConfiguration configuration,
        string httpClientName = null,
        IReadOnlyDictionary<string, string> headers = null)
        where TInterface : class
        where TClient : class, TInterface
        where TOptions : BaseServiceConfigurationOptions, new()
        where TAuthHeaderHandler : DelegatingHandler
    {
        return Register<TInterface, TClient, TOptions>(
            services,
            configuration,
            httpClientName,
            headers,
            b => b.AddAuthorizationHandler<TAuthHeaderHandler>());
    }

    private static ClientBuilder<TInterface, TOptions> Register<TInterface, TClient, TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        string httpClientName,
        IReadOnlyDictionary<string, string> headers,
        Action<IHttpClientBuilder> addHandler)
        where TInterface : class
        where TClient : class, TInterface
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        TOptions options = services.BindOptions<TOptions>(configuration, headers);
        IHttpClientBuilder builder = services
            .AddHttpClient<TInterface, TClient>(httpClientName ?? typeof(TInterface).Name)
            .ConfigureDefaults(options);
        addHandler?.Invoke(builder);

        if (typeof(IHealthCheckableApiClient).IsAssignableFrom(typeof(TClient)))
        {
            services.AddHealthCheckable<TInterface>(c => (IHealthCheckableApiClient)c);
        }

        return new ClientBuilder<TInterface, TOptions>(builder, options);
    }
}
