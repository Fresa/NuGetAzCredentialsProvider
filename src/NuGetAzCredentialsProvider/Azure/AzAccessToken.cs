using Azure.Core;
using Azure.Identity;

namespace NuGetAzCredentialsProvider.Azure;

internal static class AzAccessToken
{
    private const string ScopeVariable = "NUGETAZCREDENTIALSPROVIDER_SCOPE";

    // Azure DevOps' well-known Entra application id, as a '/.default' scope.
    private const string AzureDevOpsResourceScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";

    public static async Task<AzAccessTokenResult> GetAsync(CancellationToken cancellationToken)
    {
        var credential = new AzureCliCredential();
        var tokenRequestContext = new TokenRequestContext([GetScope()]);

        try
        {
            var accessToken = await credential
                .GetTokenAsync(tokenRequestContext, cancellationToken)
                .ConfigureAwait(false);

            return new AzAccessTokenResult.Acquired(accessToken.Token);
        }
        catch (CredentialUnavailableException exception)
        {
            return new AzAccessTokenResult.Failed(
                $"Could not get an Azure CLI token. Run 'az login'. {exception.Message}");
        }
        catch (AuthenticationFailedException exception)
        {
            return new AzAccessTokenResult.Failed(
                $"Azure CLI authentication failed. Run 'az login'. {exception.Message}");
        }
    }

    private static string GetScope() =>
        Environment.GetEnvironmentVariable(ScopeVariable) is { } scope
        && !string.IsNullOrWhiteSpace(scope)
            ? scope.Trim()
            : AzureDevOpsResourceScope;
}
