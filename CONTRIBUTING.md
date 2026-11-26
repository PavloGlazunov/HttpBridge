# Contributing to HttpBridge

Thanks for helping. All changes go through a pull request and need the maintainer's approval before they are merged.
Direct pushes to `main` and release tags are restricted.

## Workflow

1. Fork the repository and create a branch from `main`.
2. Make the change. Keep it focused: one fix or feature per pull request.
3. Add or update tests in `tests/HttpBridge.Tests` for any behavior change.
4. Make sure everything passes locally (see below).
5. Open a pull request against `main` and describe what changed and why.
   CI must be green. A workflow run from a first-time contributor needs the maintainer's approval to start.

## Build and test

Requires the .NET 10 SDK.

```powershell
dotnet build HttpBridge.slnx
dotnet test
```

The build must finish with 0 warnings.

## Generated clients

`HttpBridge.Refitter` and `HttpBridge.SwaggerCodeGen` contain committed generated code. Do not edit `*.g.cs` by hand.
Change the OpenAPI document or generator config and regenerate (commands are in the README of each package):

```powershell
dotnet tool restore
```

## Guidelines

- Keep the feature set identical across library packages. A change to shared behavior (options, resilience, health,
  OData filter) belongs in `HttpBridge.Core` and is covered by tests for each affected library package.
- Update the README of the package you change when public behavior changes.
- Use clear commit messages, for example `fix: ...`, `feat: ...`, `docs: ...`.

## Releases

Only the maintainer publishes packages: pushing a `v*` tag runs the publish workflow. Please do not bump versions in
pull requests.

## License

By contributing you agree that your contribution is licensed under the [MIT License](LICENSE).
