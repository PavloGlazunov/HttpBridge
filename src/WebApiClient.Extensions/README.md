# WebApiClient.Extensions

Helpers for building HTTP clients with [WebApiClientCore](https://github.com/dotnetcore/WebApiClient): registration from
`appsettings.json`, System.Text.Json (camelCase), retry / circuit breaker via `Microsoft.Extensions.Http.Resilience`,
optional authorization handler and health endpoints. Targets .NET 10.

## Usage

```csharp
public interface IGoogleClient : IHealthCheckableWebApiClient
{
    [HttpGet("search")]
    Task<string> SearchAsync(string q);
}

public class GoogleConfigurationOptions : BaseServiceConfigurationOptions;   // section "Google"
```
```json
{ "Google": { "BaseUrl": "https://google.com/", "Headers": { "Auth": "token" }, "MaxRetry": 5 } }
```
```csharp
services.AddWebApiClient<IGoogleClient, GoogleConfigurationOptions>(configuration)
    .WithPolicy();

services.AddWebApiClientWithAuthorization<IGoogleClient, GoogleConfigurationOptions, MyAuthHandler>(configuration)
    .WithPolicy();
```

`headers:` argument is used only when the configuration has no headers. There is no `httpClientName` argument:
WebApiClientCore names the HttpClient after the interface.

## Health checks

Derive from `IHealthCheckableWebApiClient` (`health`, `health/deep`, `{prefix}/health`, `{prefix}/health/deep`).
The client is registered as `IHealthCheckableApiClient` automatically.

```csharp
HealthCheckResult r = await client.GetHealthAsync();             // extension method
HealthCheckResult d = await client.GetDeepHealthAsync("prefix");
```

The runtime proxy sends every interface member through the HTTP pipeline, so default interface methods do not work;
`GetHealthAsync` / `GetDeepHealthAsync` are extension methods and cannot be called from inside your own interface.
