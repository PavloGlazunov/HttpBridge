using Refit;
using HttpBridge.Core;
using HttpBridge.Core.Infrastructure;
using HttpBridge.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBridge.Refit;

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
        ReflectionSupport.EnsureRegistered(); // Refit 17: reflection-based JSON needs Refit.Reflection
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
