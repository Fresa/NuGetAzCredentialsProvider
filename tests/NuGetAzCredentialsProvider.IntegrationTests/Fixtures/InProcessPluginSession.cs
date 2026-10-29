using System.Text;
using NuGet.Protocol.Plugins;

namespace NuGetAzCredentialsProvider.IntegrationTests.Fixtures;

internal sealed class InProcessPluginSession : IAsyncDisposable
{
    private readonly StandardStreamRedirection _redirection;
    private readonly StreamWriter _toPluginWriter;
    private readonly Connection _connection;
    private readonly Task<int> _pluginMain;
    private readonly string? _originalPath;

    private InProcessPluginSession(
        StandardStreamRedirection redirection,
        StreamWriter toPluginWriter,
        Connection connection,
        Task<int> pluginMain,
        string? originalPath)
    {
        _redirection = redirection;
        _toPluginWriter = toPluginWriter;
        _connection = connection;
        _pluginMain = pluginMain;
        _originalPath = originalPath;
    }

    public IConnection Client => _connection;

    public static async Task<InProcessPluginSession> StartAsync(string? pathOverride, CancellationToken cancellationToken)
    {
        var originalPath = pathOverride is null ? null : Environment.GetEnvironmentVariable("PATH");
        if (pathOverride is not null)
        {
            Environment.SetEnvironmentVariable("PATH", pathOverride);
        }

        var redirection = StandardStreamRedirection.Install();
        var pluginMain = Task.Run(() => Program.Main(["-Plugin"]), cancellationToken);

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var toPluginWriter = new StreamWriter(redirection.ToPlugin, utf8) { AutoFlush = true };
        var fromPluginReader = new StreamReader(redirection.FromPlugin, utf8);

        var connection = new Connection(
            new MessageDispatcher(new RequestHandlers(), new UniqueIdGenerator()),
            new Sender(toPluginWriter),
            new StandardInputReceiver(fromPluginReader),
            ConnectionOptions.CreateDefault());

        await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

        return new InProcessPluginSession(redirection, toPluginWriter, connection, pluginMain, originalPath);
    }

    public async ValueTask DisposeAsync()
    {
        // Close is how NuGet ends a plugin
        // https://learn.microsoft.com/en-us/nuget/reference/extensibility/nuget-cross-platform-plugins#protocol-messages-index
        await _connection
            .SendAsync(
                MessageUtilities.Create(
                    new UniqueIdGenerator().GenerateUniqueId(),
                    MessageType.Request,
                    MessageMethod.Close),
                CancellationToken.None)
            .ConfigureAwait(false);
        await _pluginMain.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        await _toPluginWriter.DisposeAsync().ConfigureAwait(false);
        _connection.Dispose();
        _redirection.Dispose();
        if (_originalPath is not null)
        {
            Environment.SetEnvironmentVariable("PATH", _originalPath);
        }
    }

    private sealed class UniqueIdGenerator : IIdGenerator
    {
        public string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
