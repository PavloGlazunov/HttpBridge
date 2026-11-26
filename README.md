# http-libraries-example

Shared NuGet packages (.NET 10) that give every popular .NET HTTP client library the same set of features:

- registration from `appsettings.json` (`BaseUrl`, `Headers`, `Timeout`, ...)
- retry + circuit breaker + timeouts via `Microsoft.Extensions.Http.Resilience` (`.WithPolicy()`)
- optional authorization `DelegatingHandler`
- `IHealthCheckableApiClient` with `/health` and `/health/deep` (optionally prefixed), resolvable as `IEnumerable<IHealthCheckableApiClient>`
- System.Text.Json (camelCase) everywhere
- OData `$filter` builder (in `Http.Extensions.Core`)

| Library | Package | Style |
|---|---|---|
| (shared) | [Http.Extensions.Core](src/Http.Extensions.Core/README.md) | options, resilience, health, OData filter |
| RestSharp | [RestSharp.Extensions](src/RestSharp.Extensions/README.md) | typed client base class |
| Flurl.Http | [Flurl.Http.Extensions](src/Flurl.Http.Extensions/README.md) | typed client base class |
| RestEase | [RestEase.Extensions](src/RestEase.Extensions/README.md) | interface |
| Refit | [Refit.Extensions](src/Refit.Extensions/README.md) | interface |
| Refitter | [Refitter.Extensions](src/Refitter.Extensions/README.md) | Refit client generated from OpenAPI |
| WebApiClient | [WebApiClient.Extensions](src/WebApiClient.Extensions/README.md) | interface |
| Swagger CodeGen | [SwaggerCodeGen.Extensions](src/SwaggerCodeGen.Extensions/README.md) | HttpClient client generated from OpenAPI (NSwag) |

## Build, test, pack

```powershell
dotnet build HttpLibraries.slnx
dotnet test
dotnet pack HttpLibraries.slnx -c Release -o artifacts
```

Generated clients are committed. Regenerate with `dotnet tool restore`, then the commands in the Refitter and
SwaggerCodeGen READMEs.
