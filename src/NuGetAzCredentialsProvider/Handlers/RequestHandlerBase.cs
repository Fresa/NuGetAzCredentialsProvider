using NuGet.Protocol.Plugins;

namespace NuGetAzCredentialsProvider.Handlers;

internal abstract class RequestHandlerBase<TRequest, TResponse>(TimeSpan? progressInterval = null) : IRequestHandler
    where TRequest : class
    where TResponse : class
{
    public CancellationToken CancellationToken => CancellationToken.None;

    public async Task HandleResponseAsync(
        IConnection connection,
        Message message,
        IResponseHandler responseHandler,
        CancellationToken cancellationToken)
    {
        var request = MessageUtilities.DeserializePayload<TRequest>(message)
            ?? throw new InvalidOperationException($"{message.Method} arrived with no payload.");

        // Each request has a timeout of 5 seconds. If operations are supposed to take
        // longer the respective process should send out a progress message to prevent
        // the request from timing out.
        // https://learn.microsoft.com/en-us/nuget/reference/extensibility/nuget-cross-platform-plugins#client---plugin-interaction
        using var progressReporter = progressInterval is { } interval
            ? AutomaticProgressReporter.Create(connection, message, interval, cancellationToken)
            : null;

        var response = await HandleRequestAsync(request, cancellationToken).ConfigureAwait(false);
        await responseHandler.SendResponseAsync(message, response, CancellationToken.None)
            .ConfigureAwait(false);
    }

    protected abstract Task<TResponse> HandleRequestAsync(
        TRequest request,
        CancellationToken cancellationToken);
}
