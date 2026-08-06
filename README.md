## Consuming the NuGet Package

`SCIRE.Foundation.Abstractions` is published through the `net-cscience` GitHub Packages feed.

> GitHub Packages requires authentication for NuGet package downloads, even when the package is public.

### 1. Create a GitHub access token

Create a **Personal Access Token (classic)** in your personal GitHub account:

1. Open **GitHub → Settings**
2. Open **Developer settings**
3. Select **Personal access tokens → Tokens (classic)**
4. Select **Generate new token (classic)**
5. Enable the following scope:

```text
read:packages
```

Copy the token when it is generated. GitHub displays it only once.

### 2. Register the GitHub Packages feed

Run the following commands in PowerShell:

```powershell
$env:GITHUB_PACKAGES_TOKEN = "<YOUR_CLASSIC_PAT>"

dotnet nuget add source `
    "https://nuget.pkg.github.com/net-cscience/index.json" `
    --name "scire-github" `
    --username "net-cscience-raphael" `
    --password $env:GITHUB_PACKAGES_TOKEN `
    --valid-authentication-types basic
```

The feed belongs to the `net-cscience` organization, while authentication uses the personal GitHub account `net-cscience-raphael`.

Verify the registered source:

```powershell
dotnet nuget list source
```

The output should contain:

```text
scire-github [Enabled]
https://nuget.pkg.github.com/net-cscience/index.json
```

When the source already exists, update its credentials instead:

```powershell
$env:GITHUB_PACKAGES_TOKEN = "<YOUR_CLASSIC_PAT>"

dotnet nuget update source "scire-github" `
    --source "https://nuget.pkg.github.com/net-cscience/index.json" `
    --username "net-cscience-raphael" `
    --password $env:GITHUB_PACKAGES_TOKEN `
    --valid-authentication-types basic
```

### 3. Add the package

Add the package to a project:

```powershell
dotnet add `
    path/to/YourProject.csproj `
    package SCIRE.Foundation.Abstractions `
    --version 0.1.0-alpha.2
```

For example:

```powershell
dotnet add `
    src/LectureCatalogInsights.Core/LectureCatalogInsights.Core.csproj `
    package SCIRE.Foundation.Abstractions `
    --version 0.1.0-alpha.2
```

This creates the following project reference:

```xml
<ItemGroup>
    <PackageReference Include="SCIRE.Foundation.Abstractions"
                      Version="0.1.0-alpha.2"/>
</ItemGroup>
```

### 4. Restore and verify

```powershell
dotnet restore
dotnet build
```

The temporary package test API can be called with:

```csharp
using SCIRE.Foundation.Abstractions.Diagnostics;

var message = FoundationHelloWorld.GetMessage();
Console.WriteLine(message);
```

Expected output:

```text
Hello from SCIRE.Foundation.Abstractions
```

### Security

Never commit a GitHub access token to the repository, project files, workflow files, or a shared `NuGet.config`.

The environment variable used above exists only for the current PowerShell session:

```powershell
$env:GITHUB_PACKAGES_TOKEN = "<YOUR_CLASSIC_PAT>"
```
