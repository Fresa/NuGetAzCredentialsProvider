using NuGet.Protocol.Plugins;

namespace NuGetAzCredentialsProvider.Handlers;

internal sealed class GetOperationClaimsRequestHandler
    : RequestHandlerBase<GetOperationClaimsRequest, GetOperationClaimsResponse>
{
    protected override Task<GetOperationClaimsResponse> HandleRequestAsync(
        GetOperationClaimsRequest request,
        CancellationToken cancellationToken)
    {
        // If the service index and package source are null, then the plugin can answer with authentication.
        // https://learn.microsoft.com/en-us/nuget/reference/extensibility/nuget-cross-platform-plugins
        OperationClaim[] claims =
            request.ServiceIndexJson is null && request.PackageSourceRepository is null
                ? [OperationClaim.Authentication]
                : [];

        return Task.FromResult(new GetOperationClaimsResponse(claims));
    }
}
