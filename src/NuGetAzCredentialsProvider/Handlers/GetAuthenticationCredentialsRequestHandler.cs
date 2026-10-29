using NuGet.Protocol.Plugins;
using NuGetAzCredentialsProvider.Azure;

namespace NuGetAzCredentialsProvider.Handlers;

internal sealed class GetAuthenticationCredentialsRequestHandler()
    : RequestHandlerBase<GetAuthenticationCredentialsRequest, GetAuthenticationCredentialsResponse>(ProgressInterval)
{
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(2);

    protected override async Task<GetAuthenticationCredentialsResponse> HandleRequestAsync(
        GetAuthenticationCredentialsRequest request,
        CancellationToken cancellationToken)
    {
        var uri = request.Uri;
        if (uri is null || !AzureDevOpsFeed.IsFor(uri))
        {
            return NotFound();
        }

        var accessToken = await AzAccessToken.GetAsync(cancellationToken)
            .ConfigureAwait(false);

        return accessToken switch
        {
            AzAccessTokenResult.Acquired acquired => Basic(acquired.Token),
            AzAccessTokenResult.Failed failed => Error(failed.Message),
            _ => throw new InvalidOperationException("Unhandled az access-token result.")
        };
    }

    private static GetAuthenticationCredentialsResponse Basic(string token) =>
        new(
            username: "NuGetAzCredentialsProvider",
            password: token,
            message: null,
            authenticationTypes: ["Basic"],
            responseCode: MessageResponseCode.Success);

    private static GetAuthenticationCredentialsResponse Error(string message) =>
        new(
            username: null,
            password: null,
            message: message,
            authenticationTypes: null,
            responseCode: MessageResponseCode.Error);

    private static GetAuthenticationCredentialsResponse NotFound() =>
        new(
            username: null,
            password: null,
            message: null,
            authenticationTypes: null,
            responseCode: MessageResponseCode.NotFound);
}
