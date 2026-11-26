# Refitter.Extensions

Refit client generated from an OpenAPI document with [Refitter](https://github.com/christianhelle/refitter), wired up with
`Refit.Extensions`: configuration binding, resilience policies, authorization handler, health endpoints. Targets .NET 10.

The package ships a working example generated from the Swagger Petstore (`openapi/petstore.json`):

```csharp
services.AddPetstoreClient(configuration)   // section "Petstore"
    .WithPolicy();

Pet pet = await serviceProvider.GetRequiredService<IPetstoreApi>().GetPetById(1);
```
```json
{ "Petstore": { "BaseUrl": "https://petstore3.swagger.io/api/v3/", "MaxRetry": 3 } }
```

`IPetstoreApi` is a partial interface: the generated half is `Generated/PetstoreApi.g.cs`, the other half (`PetstoreApi.cs`)
adds `IHealthCheckableRefitClient` and the options class.

## Use it for your own API

1. Put the OpenAPI document in `openapi/` and copy `.refitter`, changing `openApiPath`, `namespace`, `interfaceName`.
2. Install the tool once: `dotnet tool install Refitter` (this repo pins it in `.config/dotnet-tools.json`).
3. Generate (the generated file is committed, there is no build-time generation):
   ```powershell
   dotnet tool run refitter --settings-file .refitter
   ```
4. Add a partial declaration so the client gets health endpoints and register it with `AddRefitClient<IYourApi, YourConfigurationOptions>(configuration)`.
