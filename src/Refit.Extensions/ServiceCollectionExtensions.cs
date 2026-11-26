using Http.Extensions.Core;
using Http.Extensions.Core.Infrastructure;
using Http.Extensions.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Refit.Extensions;

public static class ServiceCollectionExtensions
{
    public static ClientBuilder<TClient, TOptions> AddRefitClient<TClient, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string httpClientName = null,
        IReadOnlyDictionary<string, string> headers = null,
        RefitSettings refitSettings = null)
        where TClient : class
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        return Register<TClient, TOptions>(services, configuration, httpClientName, headers, refitSettings, null);
    }

    public static ClientBuilder<TClient, TOptions> AddRefitClientWithAuthorization<TClient, TOptions, TAuthHeaderHandler>(
        this IServiceCollection services,
        IConfiguration configuration,
        string httpClientName = null,
        IReadOnlyDictionary<string, string> headers = null,
        RefitSettings refitSettings = null)
        where TClient : class
        where TOptions : BaseServiceConfigurationOptions, new()
        where TAuthHeaderHandler : DelegatingHandler
    {
        return Register<TClient, TOptions>(
            services,
            configuration,
            httpClientName,
            headers,
            refitSettings,
            b => b.AddAuthorizationHandler<TAuthHeaderHandler>());
    }

    private static ClientBuilder<TClient, TOptions> Register<TClient, TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        string httpClientName,
        IReadOnlyDictionary<string, string> headers,
        RefitSettings refitSettings,
        Action<IHttpClientBuilder> addHandler)
        where TClient : class
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        TOptions options = services.BindOptions<TOptions>(configuration, headers);
        refitSettings ??= new RefitSettings(
            new SystemTextJsonContentSerializer(JsonSerializationSettingsBuilder.CreateDefaultJsonOptions()));

        IHttpClientBuilder builder = services
            .AddRefitGeneratedClient<TClient>(_ => refitSettings, httpClientName)
            .ConfigureDefaults(options);
        addHandler?.Invoke(builder);
        if (typeof(IHealthCheckableRefitClient).IsAssignableFrom(typeof(TClient)))
        {
            services.AddHealthCheckable<TClient>(c =>
            {
                var health = (IHealthCheckableRefitClient)c;
                return new HealthCheckableAdapter(health.GetHealthAsync, health.GetDeepHealthAsync);
            });
        }

        return new ClientBuilder<TClient, TOptions>(builder, options);
    }
}
