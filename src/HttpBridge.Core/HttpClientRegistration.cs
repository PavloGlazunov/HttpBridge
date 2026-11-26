using HttpBridge.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBridge.Core;

/// <summary>Steps shared by every library package: bind options, configure HttpClient, auth handler, health registration.</summary>
public static class HttpClientRegistration
{
    /// <summary>
    /// Binds section "{TOptions name without ConfigurationOptions}" and registers it as IOptions. Requires an absolute
    /// BaseUrl and normalises it to end with "/".
    /// </summary>
    public static TOptions BindOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyDictionary<string, string> headers = null)
        where TOptions : BaseServiceConfigurationOptions, new()
    {
        IConfigurationSection section =
            configuration.GetSection(typeof(TOptions).Name.Replace("ConfigurationOptions", string.Empty));
        TOptions options = section.Get<TOptions>() ?? new TOptions();

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri baseUri) || baseUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                $"Configuration section '{section.Path}' must define an absolute http(s) BaseUrl (got '{options.BaseUrl}').");
        }

        // relative routes ("health") only keep the base path ("/api/v1/") when it ends with a slash
        options.BaseUrl = options.BaseUrl.TrimEnd('/') + "/";
        services.Configure<TOptions>(section);

        if (headers is not null && options.Headers is null or { Count: 0 })
        {
            options.Headers = headers;
        }

        return options;
    }

    public static IHttpClientBuilder ConfigureDefaults(
        this IHttpClientBuilder builder,
        BaseServiceConfigurationOptions options)
    {
        return builder.ConfigureHttpClient(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.Timeout);
            foreach (KeyValuePair<string, string> header in options.Headers ?? new Dictionary<string, string>())
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        });
    }

    public static IHttpClientBuilder AddAuthorizationHandler<THandler>(this IHttpClientBuilder builder)
        where THandler : DelegatingHandler
    {
        builder.Services.AddTransient<THandler>();
        return builder.AddHttpMessageHandler<THandler>();
    }

    /// <summary>Makes TClient resolvable as <see cref="IHealthCheckableApiClient"/> when it implements it.</summary>
    public static IServiceCollection AddHealthCheckable<TClient>(this IServiceCollection services)
        where TClient : class
    {
        if (typeof(IHealthCheckableApiClient).IsAssignableFrom(typeof(TClient)))
        {
            services.AddTransient(sp => (IHealthCheckableApiClient)sp.GetRequiredService<TClient>());
        }

        return services;
    }

    /// <summary>Registers an adapter for proxies that cannot implement <see cref="IHealthCheckableApiClient"/> themselves.</summary>
    public static IServiceCollection AddHealthCheckable<TClient>(
        this IServiceCollection services,
        Func<TClient, IHealthCheckableApiClient> adapter)
        where TClient : class
    {
        return services.AddTransient(sp => adapter(sp.GetRequiredService<TClient>()));
    }
}
