using Http.Extensions.Core;
using Http.Extensions.Core.Infrastructure;
using Http.Extensions.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RestEase.Extensions;

public static class ServiceCollectionExtensions
{
    public static ClientBuilder<TClient, TOptions> AddRestEaseClient<TClient, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string httpClientName = null,
        IReadOnlyDictionary<string, string> headers = null)
        where TClient : class
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        return Register<TClient, TOptions>(services, configuration, httpClientName, headers, null);
    }

    public static ClientBuilder<TClient, TOptions> AddRestEaseClientWithAuthorization<TClient, TOptions, TAuthHeaderHandler>(
        this IServiceCollection services,
        IConfiguration configuration,
        string httpClientName = null,
        IReadOnlyDictionary<string, string> headers = null)
        where TClient : class
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
        where TClient : class
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        TOptions options = services.BindOptions<TOptions>(configuration, headers);
        var json = JsonSerializationSettingsBuilder.CreateDefaultJsonOptions();

        IHttpClientBuilder builder = services
            .AddHttpClient(httpClientName ?? typeof(TClient).Name)
            .ConfigureDefaults(options);
        addHandler?.Invoke(builder);
        builder.AddTypedClient(http => new RestClient(http)
        {
            RequestBodySerializer = new SystemTextJsonRequestBodySerializer(json),
            ResponseDeserializer = new SystemTextJsonResponseDeserializer(json),
        }.For<TClient>());

        if (typeof(IHealthCheckableRestEaseClient).IsAssignableFrom(typeof(TClient)))
        {
            services.AddHealthCheckable<TClient>(c =>
            {
                var health = (IHealthCheckableRestEaseClient)c;
                return new HealthCheckableAdapter(p => health.GetHealthAsync(p), p => health.GetDeepHealthAsync(p));
            });
        }

        return new ClientBuilder<TClient, TOptions>(builder, options);
    }
}
