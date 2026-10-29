using System.Text;
using NuGet.Protocol.Plugins;

namespace NuGetAzCredentialsProvider.ComponentTests.Fixtures;

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
        await _toPluginWriter.DisposeAsync().ConfigureAwait(false);
        try
        {
            await _pluginMain.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
        }

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