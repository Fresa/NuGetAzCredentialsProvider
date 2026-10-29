using NuGet.Protocol.Plugins;

namespace NuGetAzCredentialsProvider.Handlers;

internal sealed class SetCredentialsRequestHandler
    : RequestHandlerBase<SetCredentialsRequest, SetCredentialsResponse>
{
    protected override Task<SetCredentialsResponse> HandleRequestAsync(
        SetCredentialsRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new SetCredentialsResponse(MessageResponseCode.Success));
}
