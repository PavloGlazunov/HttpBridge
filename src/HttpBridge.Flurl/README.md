# HttpBridge.Flurl

Helpers for building HTTP clients with [Flurl.Http](https://flurl.dev): registration from `appsettings.json`,
System.Text.Json (camelCase), retry / circuit breaker via `Microsoft.Extensions.Http.Resilience`, optional authorization
handler and health endpoints. The `FlurlClient` wraps the `HttpClient` created by `IHttpClientFactory`, so all handlers
(resilience, authorization) apply. Targets .NET 10.

## Usage

```csharp
public class GoogleClient(HttpClient httpClient) : FlurlClientBase(httpClient)
{
    public Task<string> SearchAsync(string q) =>
        Client.Request("search").SetQueryParam("q", q).GetStringAsync();
}

public class GoogleConfigurationOptions : BaseServiceConfigurationOptions;   // section "Google"
```
```json
{ "Google": { "BaseUrl": "https://google.com/", "Headers": { "Auth": "token" }, "MaxRetry": 5 } }
```
```csharp
services.AddFlurlClient<GoogleClient, GoogleConfigurationOptions>(configuration)
    .WithPolicy();

services.AddFlurlClientWithAuthorization<GoogleClient, GoogleConfigurationOptions, MyAuthHandler>(configuration, "googleApi")
    .WithPolicy();
```

`headers:` argument is used only when the configuration has no headers.

## Health checks

`FlurlClientBase` implements `IHealthCheckableApiClient` (`health`, `health/deep`, optional `{prefix}/...`) and is
registered as such automatically.

```csharp
HealthCheckResult r = await client.GetHealthAsync();
HealthCheckResult d = await client.GetDeepHealthAsync("prefix");
```
