# SwaggerCodeGen.Extensions

`HttpClient` client generated from an OpenAPI / Swagger document with [NSwag](https://github.com/RicoSuter/NSwag)
(.NET tool, no Java needed), wired up with configuration binding, resilience policies, authorization handler and health
endpoints. Targets .NET 10.

The package ships a working example generated from the Swagger Petstore (`openapi/petstore.json`):

```csharp
services.AddPetstoreClient(configuration)   // section "Petstore"
    .WithPolicy();

Pet pet = await serviceProvider.GetRequiredService<IPetstoreClient>().GetPetByIdAsync(1);
```
```json
{ "Petstore": { "BaseUrl": "https://petstore3.swagger.io/api/v3/", "MaxRetry": 3 } }
```

`PetstoreClient` is a partial class: the generated half is `Generated/PetstoreClient.g.cs`; `PetstoreClient.cs` plugs in
System.Text.Json defaults (camelCase, enums as strings) and `IHealthCheckableApiClient`.

## Use it for your own API

1. Put the OpenAPI document in `openapi/` and copy `nswag.json`, changing `url`, `className`, `namespace`, `output`.
2. Generate (the generated file is committed, there is no build-time generation):
   ```powershell
   dotnet tool run nswag run nswag.json
   ```
3. Register any generated client with a constructor taking `HttpClient`:
   ```csharp
   services.AddSwaggerClient<IMyClient, MyClient, MyConfigurationOptions>(configuration).WithPolicy();
   services.AddSwaggerClientWithAuthorization<IMyClient, MyClient, MyConfigurationOptions, MyAuthHandler>(configuration);
   ```
   Add a partial class implementing `IHealthCheckableApiClient` (see `PetstoreClient.cs`) to get health endpoints.
