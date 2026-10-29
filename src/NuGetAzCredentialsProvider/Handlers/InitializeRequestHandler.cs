using NuGet.Protocol.Plugins;

namespace NuGetAzCredentialsProvider.Handlers;

internal sealed class InitializeRequestHandler
    : RequestHandlerBase<InitializeRequest, InitializeResponse>
{
    protected override Task<InitializeResponse> HandleRequestAsync(
        InitializeRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new InitializeResponse(MessageResponseCode.Success));
}
