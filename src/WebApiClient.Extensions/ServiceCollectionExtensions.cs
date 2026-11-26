using Http.Extensions.Core;
using Http.Extensions.Core.Infrastructure;
using Http.Extensions.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiClient.Extensions;

public static class ServiceCollectionExtensions
{
    /// <remarks>WebApiClientCore names the underlying HttpClient after the interface, so there is no httpClientName argument.</remarks>
    public static ClientBuilder<TClient, TOptions> AddWebApiClient<TClient, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyDictionary<string, string> headers = null)
        where TClient : class
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        return Register<TClient, TOptions>(services, configuration, headers, null);
    }

    public static ClientBuilder<TClient, TOptions> AddWebApiClientWithAuthorization<TClient, TOptions, TAuthHeaderHandler>(
        this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyDictionary<string, string> headers = null)
        where TClient : class
        where TOptions : BaseServiceConfigurationOptions, new()
        where TAuthHeaderHandler : DelegatingHandler
    {
        return Register<TClient, TOptions>(
            services,
            configuration,
            headers,
            b => b.AddAuthorizationHandler<TAuthHeaderHandler>());
    }

    private static ClientBuilder<TClient, TOptions> Register<TClient, TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyDictionary<string, string> headers,
        Action<IHttpClientBuilder> addHandler)
        where TClient : class
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        TOptions options = services.BindOptions<TOptions>(configuration, headers);

        IHttpClientBuilder builder = services
            .AddHttpApi<TClient>(o =>
            {
                o.HttpHost = new Uri(options.BaseUrl);
                JsonSerializationSettingsBuilder.SetDefaultJsonOptions(o.JsonSerializeOptions);
                JsonSerializationSettingsBuilder.SetDefaultJsonOptions(o.JsonDeserializeOptions);
            })
            .ConfigureDefaults(options);
        addHandler?.Invoke(builder);

        if (typeof(IHealthCheckableWebApiClient).IsAssignableFrom(typeof(TClient)))
        {
            services.AddHealthCheckable<TClient>(c =>
            {
                var health = (IHealthCheckableWebApiClient)c;
                return new HealthCheckableAdapter(p => health.GetHealthAsync(p), p => health.GetDeepHealthAsync(p));
            });
        }

        return new ClientBuilder<TClient, TOptions>(builder, options);
    }
}
