using NuGet.Protocol.Plugins;

namespace NuGetAzCredentialsProvider.Handlers;

internal sealed class SetLogLevelRequestHandler
    : RequestHandlerBase<SetLogLevelRequest, SetLogLevelResponse>
{
    protected override Task<SetLogLevelResponse> HandleRequestAsync(
        SetLogLevelRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new SetLogLevelResponse(MessageResponseCode.Success));
}
