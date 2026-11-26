# Refit.Extensions

Helpers for building HTTP clients with [Refit](https://github.com/reactiveui/refit): registration from `appsettings.json`,
retry / circuit breaker via `Microsoft.Extensions.Http.Resilience`, optional authorization handler, health endpoints,
OData filter builder (in `Http.Extensions.Core`).

Targets .NET 10, Refit 17.

## Usage

1. Define the client interface
   ```csharp
   public interface IGoogleClient
   {
       [Get("/search")]
       Task<ApiResponse<string>> SearchAsync([Query] SearchParams searchParams);
   }
   ```
2. Create options. The section name is the class name without `ConfigurationOptions` (`Google`).
   ```csharp
   public class GoogleConfigurationOptions : BaseServiceConfigurationOptions;
   ```
   ```json
   { "Google": { "BaseUrl": "https://google.com/", "Headers": { "Auth": "token" }, "MaxRetry": 5 } }
   ```
3. Register
   ```csharp
   // headers argument is used only when the configuration has none
   services.AddRefitClient<IGoogleClient, GoogleConfigurationOptions>(configuration,
           headers: new Dictionary<string, string> { ["function-key"] = "some-value" })
       .WithPolicy();   // retry + circuit breaker + timeouts from the options

   // authorization handler (DelegatingHandler) and named HttpClient
   services.AddRefitClientWithAuthorization<IGoogleClient, GoogleConfigurationOptions, MyAuthHandler>(configuration, "googleApi")
       .WithPolicy();
   ```

`AddRefitClient` returns `ClientBuilder<TClient, TOptions>`; `.HttpClientBuilder` exposes the underlying `IHttpClientBuilder`.

> Refit 17 replaced `AddRefitClient` with the source-generated `AddRefitGeneratedClient`. This package wraps it, so the
> consuming code does not change.

## Health checks

Derive the client interface from `IHealthCheckableRefitClient` to get `/health` and `/health/deep`
(or `/{prefix}/health`, `/{prefix}/health/deep`). It is registered automatically as `IHealthCheckableApiClient`.

```csharp
public interface IRestApiClient : IHealthCheckableRefitClient
{
    Task<HealthCheckResult> GetCustomHealthAsync() => GetHealthAsync("prefix");
}

public class HealthController(IEnumerable<IHealthCheckableApiClient> clients)
{
    public async Task<HealthCheckResult[]> HealthAsync() =>
        await Task.WhenAll(clients.Select(x => x.GetHealthAsync()));
}
```

## OData filter

See `Http.Extensions.Core`: `FilterQueryBuilder.CreateFilter<Invoice>(x => x.Name.Contains("test"))`.
