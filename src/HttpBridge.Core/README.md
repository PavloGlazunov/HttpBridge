# HttpBridge.Core

Shared building blocks used by the `HttpBridge.*` packages (Refit, RestEase, RestSharp, Flurl.Http, WebApiClient, ...).
Reference it directly only if you write your own library adapter.

- `BaseServiceConfigurationOptions` - base URL, headers, retry, timeout, circuit breaker settings (bound from appsettings.json).
- `ResiliencePolicies.WithPolicy` - `AddStandardResilienceHandler` configured from the options.
- `IHealthCheckableApiClient` - library-agnostic `/health` and `/health/deep` contract. Probes never throw and never return
  null: transport errors and non-success statuses become an `Unhealthy` result, a JSON body with `status` is used as is.
- `HttpClientRegistration` - option binding, HttpClient defaults, auth handler, health registration. `BaseUrl` must be an
  absolute URL (checked at registration); a trailing `/` is added so base paths such as `https://host/api/v1` are kept.
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
FilterQueryBuilder.CreateFilter<Invoice>(x => names.Contains(x.Name)); // names is string[], List<string>, HashSet<string>
// name in ('first','last')
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Client.FirstName == "a");
// client/firstName eq 'a'
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Active && (x.Age > 5 || x.Age < 1));
// active and (age gt 5 or age lt 1)
FilterQueryBuilder.CreateFilter<Invoice>(x => !x.Active);                     // not active
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Name == "O'Brien");            // name eq 'O''Brien'
FilterQueryBuilder.CreateFilter<Invoice>(x => x.Status == Status.Paid);       // status eq 'Paid'
```

Property names are camelCased, nested members use `/`. Single quotes in strings are doubled, numbers and dates are
formatted with the invariant culture (dates as UTC, `Unspecified` is treated as UTC).
`new DateTime(...)` inside the lambda is not supported - capture the value in a variable first.
