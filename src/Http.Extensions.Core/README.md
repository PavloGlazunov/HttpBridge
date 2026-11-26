# Http.Extensions.Core

Shared building blocks used by the `*.Extensions` packages (Refit, RestEase, RestSharp, Flurl.Http, WebApiClient, ...).
Reference it directly only if you write your own library adapter.

- `BaseServiceConfigurationOptions` - base URL, headers, retry, timeout, circuit breaker settings (bound from appsettings.json).
- `ResiliencePolicies.WithPolicy` - `AddStandardResilienceHandler` configured from the options.
- `IHealthCheckableApiClient` - library-agnostic `/health` and `/health/deep` contract.
- `HttpClientRegistration` - option binding, HttpClient defaults, auth handler, health registration.
- `FilterQueryBuilder` - builds OData `$filter` from a LINQ expression.

## OData filter

```csharp
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Id == Guid.Empty);
// id eq '00000000-0000-0000-0000-000000000000'
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Created > date);       // date is a captured DateTime
// created gt 1999-12-31T23:59:59.0000000Z
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Name.Contains("test"));
// contains(name,'test')
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Name.StartsWith("test"));
// startswith(name,'test')
FilterQueryBuilder.CreateFilter<Invoice>(x => names.Contains(x.Name)); // names is string[] or List<string>
// name in ('first','last')
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Client.FirstName == "a");
// client/firstName eq 'a'
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Active && (x.Age > 5 || x.Age < 1));
// active and (age gt 5 or age lt 1)
```

Property names are camelCased, nested members use `/`.
`new DateTime(...)` inside the lambda is not supported - capture the value in a variable first.
