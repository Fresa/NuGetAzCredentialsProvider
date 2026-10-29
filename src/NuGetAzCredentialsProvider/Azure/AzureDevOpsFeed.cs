namespace NuGetAzCredentialsProvider.Azure;

internal static class AzureDevOpsFeed
{
    // Semicolon separated hosts that replace the default; a leading '*.' matches any subdomain.
    private const string HostsVariable = "NUGETAZCREDENTIALSPROVIDER_HOSTS";
    private const string DefaultHost = "pkgs.dev.azure.com";
    private const string AllowHttpVariable = "NUGETAZCREDENTIALSPROVIDER_ALLOW_HTTP";

    public static bool IsFor(Uri uri) =>
        (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && IsHttpAllowed()))
        && GetHosts().Any(host => Matches(uri, host));

    private static bool IsHttpAllowed() =>
        bool.TryParse(Environment.GetEnvironmentVariable(AllowHttpVariable), out var allowHttp)
        && allowHttp;

    private static string[] GetHosts() =>
        Environment.GetEnvironmentVariable(HostsVariable) is { } hosts
        && !string.IsNullOrWhiteSpace(hosts)
            ? hosts.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [DefaultHost];

    private static bool Matches(Uri uri, string host) =>
        host.StartsWith("*.", StringComparison.Ordinal)
            ? uri.Host.EndsWith(host[1..], StringComparison.OrdinalIgnoreCase)
            : uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase);
}