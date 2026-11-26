# HttpBridge

**Goal:** make writing HTTP clients simpler, and make it easier to migrate OData clients to plain HTTP clients.

Shared NuGet packages (.NET 10) that give every popular .NET HTTP client library the same set of features:

- registration from `appsettings.json` (`BaseUrl`, `Headers`, `Timeout`, ...)
- retry + circuit breaker + timeouts via `Microsoft.Extensions.Http.Resilience` (`.WithPolicy()`)
- optional authorization `DelegatingHandler`
- `IHealthCheckableApiClient` with `/health` and `/health/deep` (optionally prefixed), resolvable as `IEnumerable<IHealthCheckableApiClient>`
- System.Text.Json (camelCase) everywhere
- OData `$filter` builder (in `HttpBridge.Core`)

Usage and registration examples for each library are in the README of its package (links in the table below).

| Library | Package | Style |
|---|---|---|
| (shared) | [HttpBridge.Core](src/HttpBridge.Core/README.md) | options, resilience, health, OData filter |
| RestSharp | [HttpBridge.RestSharp](src/HttpBridge.RestSharp/README.md) | typed client base class |
| Flurl.Http | [HttpBridge.Flurl](src/HttpBridge.Flurl/README.md) | typed client base class |
| RestEase | [HttpBridge.RestEase](src/HttpBridge.RestEase/README.md) | interface |
| Refit | [HttpBridge.Refit](src/HttpBridge.Refit/README.md) | interface |
| Refitter | [HttpBridge.Refitter](src/HttpBridge.Refitter/README.md) | Refit client generated from OpenAPI |
| WebApiClient | [HttpBridge.WebApiClient](src/HttpBridge.WebApiClient/README.md) | interface |
| Swagger CodeGen | [HttpBridge.SwaggerCodeGen](src/HttpBridge.SwaggerCodeGen/README.md) | HttpClient client generated from OpenAPI (NSwag) |

## Build, test, pack

```powershell
dotnet build HttpBridge.slnx
dotnet test
dotnet pack HttpBridge.slnx -c Release -o artifacts
```

Generated clients are committed. Regenerate with `dotnet tool restore`, then the commands in the Refitter and
SwaggerCodeGen READMEs.

## Publishing

CI (`.github/workflows/ci.yml`) builds and tests every push to `main` and every pull request.

To publish, set up nuget.org Trusted Publishing (policy for this repo and `publish.yml`) and add the repository secret `NUGET_USER` (nuget.org profile name), then push a tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

`.github/workflows/publish.yml` builds, tests and pushes all eight packages with the version taken from the tag.

## License

[MIT](LICENSE)
