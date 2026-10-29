namespace NuGetAzCredentialsProvider.ComponentTests.Fixtures;

// Writes a stand-in `az` into a temp directory that answers any `account get-access-token` with a
// fixed token, so a test exercises the token path without a real Azure CLI login. Put its directory
// first on PATH.
internal sealed class FakeAz : IDisposable
{
    private readonly TempDirectory _directory;

    private FakeAz(TempDirectory directory)
    {
        _directory = directory;
    }

    public string Path => _directory.Path;

    public static FakeAz ReturningToken(string token)
    {
        var directory = new TempDirectory();
        var tokenJson =
            $$"""{"accessToken":"{{token}}","expiresOn":"2099-12-31 23:59:59.999999","expires_on":4102444799,"subscription":"test","tenant":"test","tokenType":"Bearer"}""";

        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(System.IO.Path.Combine(directory.Path, "az.cmd"), $"@echo {tokenJson}");
        }
        else
        {
            var script = System.IO.Path.Combine(directory.Path, "az");
            File.WriteAllText(script, $"#!/bin/sh\necho '{tokenJson}'\n");
            File.SetUnixFileMode(
                script,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        return new FakeAz(directory);
    }

    public void Dispose()
    {
        _directory.Dispose();
    }
}
