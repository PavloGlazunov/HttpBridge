# RestEase.Extensions

Helpers for building HTTP clients with [RestEase](https://github.com/canton7/RestEase): registration from `appsettings.json`,
System.Text.Json (camelCase) instead of Json.NET, retry / circuit breaker via `Microsoft.Extensions.Http.Resilience`,
optional authorization handler and health endpoints. Targets .NET 10.

## Usage

```csharp
public interface IGoogleClient : IHealthCheckableRestEaseClient
{
    [Get("search")]
    Task<string> SearchAsync([Query] string q);
}

public class GoogleConfigurationOptions : BaseServiceConfigurationOptions;   // section "Google"
```
```json
{ "Google": { "BaseUrl": "https://google.com/", "Headers": { "Auth": "token" }, "MaxRetry": 5 } }
```
```csharp
services.AddRestEaseClient<IGoogleClient, GoogleConfigurationOptions>(configuration)
    .WithPolicy();

services.AddRestEaseClientWithAuthorization<IGoogleClient, GoogleConfigurationOptions, MyAuthHandler>(configuration, "googleApi")
    .WithPolicy();
```

`headers:` argument is used only when the configuration has no headers. The returned `ClientBuilder` exposes `.HttpClientBuilder`.

## Health checks

Derive from `IHealthCheckableRestEaseClient` (`health`, `health/deep`, `{prefix}/health`, `{prefix}/health/deep`).
The client is registered as `IHealthCheckableApiClient` automatically.

```csharp
HealthCheckResult r = await client.GetHealthAsync();             // extension method
HealthCheckResult d = await client.GetDeepHealthAsync("prefix");
```

RestEase rejects default interface methods, so unlike the Refit package `GetHealthAsync` / `GetDeepHealthAsync` are
extension methods and cannot be called from inside your own interface.
