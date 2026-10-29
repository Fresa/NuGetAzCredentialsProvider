# NuGetAzCredentialsProvider

NuGet credential plugin that authenticates to NuGet feeds using Azure CLI login.

## Requirements

- NuGet client 6.13 or later. The `dotnet` CLI bundles it from .NET SDK 9.0.200; check with
  `dotnet nuget --version`.
- .NET 9 runtime or later.
- Azure CLI / az

## Install

```
dotnet tool install -g NuGetAzCredentialsProvider
```

## Usage

Sign in with the Azure CLI, then restore as usual:

```
az login
dotnet restore
```

NuGet uses the plugin when a feed on a [configured host](#configuration) needs credentials.

## Configuration

The following environment variables are supported:

| Variable                                | Description                                                                                            | Default                                                        | Example                                                |
|-----------------------------------------|--------------------------------------------------------------------------------------------------------|----------------------------------------------------------------|--------------------------------------------------------|
| `NUGETAZCREDENTIALSPROVIDER_HOSTS`      | Semicolon separated hosts to authenticate. Replaces the default. A leading `*.` matches any subdomain. | `pkgs.dev.azure.com`                                           | `pkgs.dev.azure.com;*.pkgs.visualstudio.com;localhost` |
| `NUGETAZCREDENTIALSPROVIDER_ALLOW_HTTP` | `true` also authenticates `http://` feeds, meaning the token is sent unencrypted.                      | `false`                                                        | `true`                                                 |
| `NUGETAZCREDENTIALSPROVIDER_SCOPE`      | Entra scope of the token requested from the Azure CLI.                                                 | `499b84ac-1321-427f-aa17-267ca6975798/.default` (Azure DevOps) | `api://my-feed/.default`                               |

### JetBrains Rider

Settings → Build, Execution, Deployment → NuGet:

- Credential providers: set "Use credential providers" to "NuGet/.NET CLI plugins".
- Restore: keep Rider's own .NET Core restore engine (the default). The MSBuild engine does not
  support credential providers.

Restart Rider to pick up the changes.

# Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

# License
[MIT](LICENSE)
