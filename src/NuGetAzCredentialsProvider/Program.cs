using System.Runtime.InteropServices;
using NuGet.Protocol.Plugins;
using NuGetAzCredentialsProvider.Handlers;

namespace NuGetAzCredentialsProvider;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!args.Contains("-Plugin"))
        {
            // The plugins are launched with the argument "-Plugin".
            // In case a user directly launches a plugin executable
            // without this argument, the plugin can give an informative
            // message instead of waiting for a protocol handshake.
            // https://learn.microsoft.com/en-us/nuget/reference/extensibility/nuget-cross-platform-plugins#client---plugin-interaction
            await Console.Error.WriteLineAsync(
                    "NuGetAzCredentialsProvider is a NuGet credential plugin and is started by NuGet. "
                    + "See https://github.com/Fresa/NuGetAzCredentialsProvider")
                .ConfigureAwait(false);

            return 1;
        }

        using var cancellationTokenSource = new CancellationTokenSource();

        var requestHandlers = new RequestHandlers();
        requestHandlers.TryAdd(MessageMethod.Initialize, new InitializeRequestHandler());
        requestHandlers.TryAdd(MessageMethod.SetLogLevel, new SetLogLevelRequestHandler());
        requestHandlers.TryAdd(MessageMethod.SetCredentials, new SetCredentialsRequestHandler());
        requestHandlers.TryAdd(MessageMethod.GetOperationClaims, new GetOperationClaimsRequestHandler());
        requestHandlers.TryAdd(MessageMethod.GetAuthenticationCredentials, new GetAuthenticationCredentialsRequestHandler());

        using var plugin = await PluginFactory.CreateFromCurrentProcessAsync(
                requestHandlers,
                ConnectionOptions.CreateDefault(),
                cancellationTokenSource.Token)
            .ConfigureAwait(false);

        var closed = new TaskCompletionSource();
        plugin.Closed += (_, _) => closed.TrySetResult();
        plugin.Connection.Faulted += (_, _) => closed.TrySetResult();

        using var sigtermRegistration = PosixSignalRegistration.Create(
            PosixSignal.SIGTERM,
            context =>
            {
                context.Cancel = true;
                closed.TrySetResult();
            });

        await closed.Task.ConfigureAwait(false);
        await cancellationTokenSource.CancelAsync().ConfigureAwait(false);

        return 0;
    }
}