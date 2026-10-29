using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NuGetAzCredentialsProvider.IntegrationTests.Fixtures;

internal sealed partial class StandardStreamRedirection : IDisposable
{
    private readonly Action _restore;

    private StandardStreamRedirection(Stream toPlugin, Stream fromPlugin, Action restore)
    {
        ToPlugin = toPlugin;
        FromPlugin = fromPlugin;
        _restore = restore;
    }

    public Stream ToPlugin { get; }

    public Stream FromPlugin { get; }

    public static StandardStreamRedirection Install() => 
        OperatingSystem.IsWindows() ? InstallWindows() : InstallLinux();

    public void Dispose()
    {
        ToPlugin.Dispose();
        FromPlugin.Dispose();
        _restore();
    }

    // https://man7.org/linux/man-pages/man3/stdin.3.html
    private static StandardStreamRedirection InstallLinux()
    {
        // Each pipe is a pair of file descriptors: [0] is the read end, [1] the write end.
        // https://man7.org/linux/man-pages/man2/pipe.2.html
        var toPluginPipe = new int[2];
        var fromPluginPipe = new int[2];
        if (Pipe(toPluginPipe) != 0 || Pipe(fromPluginPipe) != 0)
        {
            throw new InvalidOperationException("Could not create a pipe for the plugin's stdio.");
        }

        var savedIn = Dup(0);
        var savedOut = Dup(1);
        Dup2(toPluginPipe[0], 0);
        Close(toPluginPipe[0]);
        Dup2(fromPluginPipe[1], 1);
        Close(fromPluginPipe[1]);

        // The test's ends of the pipes, as streams that own and close their descriptors.
        var toPlugin = new FileStream(new SafeFileHandle(toPluginPipe[1], ownsHandle: true), FileAccess.Write);
        var fromPlugin = new FileStream(new SafeFileHandle(fromPluginPipe[0], ownsHandle: true), FileAccess.Read);

        return new StandardStreamRedirection(toPlugin, fromPlugin, () =>
        {
            Dup2(savedIn, 0);
            Dup2(savedOut, 1);
            Close(savedIn);
            Close(savedOut);
        });
    }

    private static StandardStreamRedirection InstallWindows()
    {
        // Each pipe has a read handle and a write handle.
        // https://learn.microsoft.com/en-us/windows/win32/api/namedpipeapi/nf-namedpipeapi-createpipe
        if (!CreatePipe(out var toPluginRead, out var toPluginWrite, IntPtr.Zero, 0)
            || !CreatePipe(out var fromPluginRead, out var fromPluginWrite, IntPtr.Zero, 0))
        {
            throw new InvalidOperationException("Could not create a pipe for the plugin's stdio.");
        }

        // Keep the real stdin and stdout handles to restore later, then make the plugin's ends of the
        // pipes the process's standard handles.
        // https://learn.microsoft.com/en-us/windows/console/getstdhandle
        // https://learn.microsoft.com/en-us/windows/console/setstdhandle
        var savedIn = GetStdHandle(StdInputHandle);
        var savedOut = GetStdHandle(StdOutputHandle);
        SetStdHandle(StdInputHandle, toPluginRead.DangerousGetHandle());
        SetStdHandle(StdOutputHandle, fromPluginWrite.DangerousGetHandle());
        var toPlugin = new FileStream(toPluginWrite, FileAccess.Write);
        var fromPlugin = new FileStream(fromPluginRead, FileAccess.Read);

        return new StandardStreamRedirection(toPlugin, fromPlugin, () =>
        {
            SetStdHandle(StdInputHandle, savedIn);
            SetStdHandle(StdOutputHandle, savedOut);
            toPluginRead.Dispose();
            fromPluginWrite.Dispose();
        });
    }

    // https://learn.microsoft.com/en-us/windows/console/getstdhandle#parameters
    private const int StdInputHandle = -10;
    private const int StdOutputHandle = -11;

    // Creates a pipe and writes its read end to [0] and its write end to [1]. Returns 0 on success.
    // https://man7.org/linux/man-pages/man2/pipe.2.html
    [LibraryImport("libc", SetLastError = true, EntryPoint = "pipe")]
    private static partial int Pipe(Span<int> fileDescriptors);

    // Returns a new file descriptor referring to the same file as the given one.
    // https://man7.org/linux/man-pages/man2/dup.2.html
    [LibraryImport("libc", SetLastError = true, EntryPoint = "dup")]
    private static partial int Dup(int fileDescriptor);

    // Makes newFileDescriptor refer to the same file as oldFileDescriptor
    // https://man7.org/linux/man-pages/man2/dup.2.html
    [LibraryImport("libc", SetLastError = true, EntryPoint = "dup2")]
    private static partial int Dup2(int oldFileDescriptor, int newFileDescriptor);

    // Closes a file descriptor.
    // https://man7.org/linux/man-pages/man2/close.2.html
    [LibraryImport("libc", SetLastError = true, EntryPoint = "close")]
    private static partial int Close(int fileDescriptor);

    // Creates an anonymous pipe and returns its read and write handles.
    // https://learn.microsoft.com/en-us/windows/win32/api/namedpipeapi/nf-namedpipeapi-createpipe
    [LibraryImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreatePipe(
        out SafeFileHandle readPipe,
        out SafeFileHandle writePipe,
        IntPtr securityAttributes,
        int size);

    // Returns the process's current standard input, output or error handle.
    // https://learn.microsoft.com/en-us/windows/console/getstdhandle
    [LibraryImport("kernel32", SetLastError = true)]
    private static partial IntPtr GetStdHandle(int stdHandle);

    // Replaces the process's standard input, output or error handle.
    // https://learn.microsoft.com/en-us/windows/console/setstdhandle
    [LibraryImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetStdHandle(int stdHandle, IntPtr handle);
}
