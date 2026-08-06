# SCIRE Foundation

Shared foundation packages for SCIRE-based, feature-oriented applications.

The repository provides stable contracts and runtime infrastructure for contexts, coordinates, schemas, features, plugins, configuration, and application composition.

## Packages

| Package                                 | Purpose                                                                  |
| --------------------------------------- | ------------------------------------------------------------------------ |
| `SCIRE.Foundation.Abstractions`         | Core contracts and shared domain abstractions                            |
| `SCIRE.Foundation.Plugins.Abstractions` | Plugin descriptions and plugin-facing contracts                          |
| `SCIRE.Foundation.Runtime`              | Plugin loading, configuration, registration, and application composition |

Only `SCIRE.Foundation.Abstractions` is currently implemented.

## Structure

```text
scire-foundation/
├── src/
│   ├── SCIRE.Foundation.Abstractions/
│   ├── SCIRE.Foundation.Plugins.Abstractions/
│   └── SCIRE.Foundation.Runtime/
├── tests/
│   ├── SCIRE.Foundation.Abstractions.Tests/
│   ├── SCIRE.Foundation.Plugins.Abstractions.Tests/
│   └── SCIRE.Foundation.Runtime.Tests/
├── docs/
│   └── context/
└── SCIRE.Foundation.slnx
```

## Development

Requirements:

* .NET 10 SDK

Build and test:

```shell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Create the NuGet package:

```shell
dotnet pack src/SCIRE.Foundation.Abstractions \
    --configuration Release \
    --output artifacts/packages
```

## Consuming the Package

Packages are published through the `net-cscience` GitHub Packages feed.

GitHub requires authentication for NuGet downloads, including public packages.

Create a GitHub Personal Access Token (classic) with:

```text
read:packages
```

Register the feed:

```powershell
$env:GITHUB_PACKAGES_TOKEN = "<YOUR_CLASSIC_PAT>"

dotnet nuget add source `
    "https://nuget.pkg.github.com/net-cscience/index.json" `
    --name "scire-github" `
    --username "<YOUR_GITHUB_USERNAME>" `
    --password $env:GITHUB_PACKAGES_TOKEN `
    --valid-authentication-types basic
```

Add the package:

```powershell
dotnet add path/to/YourProject.csproj `
    package SCIRE.Foundation.Abstractions `
    --version 0.1.0-alpha.2
```

Never commit access tokens to the repository.

## Status

The project is in early development. Package APIs may change before the first stable release.
