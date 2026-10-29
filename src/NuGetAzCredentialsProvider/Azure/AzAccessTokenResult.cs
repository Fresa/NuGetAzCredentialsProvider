namespace NuGetAzCredentialsProvider.Azure;

internal abstract record AzAccessTokenResult
{
    private AzAccessTokenResult()
    {
    }

    internal sealed record Acquired(string Token) : AzAccessTokenResult;

    internal sealed record Failed(string Message) : AzAccessTokenResult;
}
