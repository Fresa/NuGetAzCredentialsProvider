using AwesomeAssertions;
using NuGet.Protocol.Plugins;
using NuGetAzCredentialsProvider.ComponentTests.Fixtures;

namespace NuGetAzCredentialsProvider.ComponentTests;

// The in-process session redirects the process's stdio and PATH, both global, so these tests must
// not overlap with any other test.
[CollectionDefinition(nameof(PluginProtocolTests), DisableParallelization = true)]
[Collection(nameof(PluginProtocolTests))]
public sealed class PluginProtocolTests
{
    private static readonly Uri AzureDevOpsFeed =
        new("https://pkgs.dev.azure.com/org/project/_packaging/feed/nuget/v3/index.json");

    private static readonly Uri NuGetOrg = new("https://api.nuget.org/v3/index.json");

    [Fact]
    public async Task GetOperationClaims_NoServiceIndexOrPackageSource_ClaimsAuthentication()
    {
        await using var session = await InProcessPluginSession.StartAsync(
            pathOverride: null,
            TestContext.Current.CancellationToken);

        var response = await session.Client
            .SendRequestAndReceiveResponseAsync<GetOperationClaimsRequest, GetOperationClaimsResponse>(
                MessageMethod.GetOperationClaims,
                new GetOperationClaimsRequest(packageSourceRepository: null, serviceIndexJson: null),
                TestContext.Current.CancellationToken);

        var claimsResponse = response.Should().BeOfType<GetOperationClaimsResponse>().Which;
        claimsResponse.Claims.Should().Contain(OperationClaim.Authentication);
    }

    [Fact]
    public async Task GetOperationClaims_ForAPackageSource_ClaimsNothing()
    {
        await using var session = await InProcessPluginSession.StartAsync(
            pathOverride: null,
            TestContext.Current.CancellationToken);

        var response = await session.Client
            .SendRequestAndReceiveResponseAsync<GetOperationClaimsRequest, GetOperationClaimsResponse>(
                MessageMethod.GetOperationClaims,
                new GetOperationClaimsRequest(
                    packageSourceRepository: AzureDevOpsFeed.AbsoluteUri,
                    serviceIndexJson: "{}"),
                TestContext.Current.CancellationToken);

        var claimsResponse = response.Should().BeOfType<GetOperationClaimsResponse>().Which;
        claimsResponse.Claims.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuthenticationCredentials_ForANonAzureDevOpsFeed_ReturnsNotFound()
    {
        await using var session = await InProcessPluginSession.StartAsync(
            pathOverride: null,
            TestContext.Current.CancellationToken);

        var response = await session.Client
            .SendRequestAndReceiveResponseAsync<GetAuthenticationCredentialsRequest, GetAuthenticationCredentialsResponse>(
                MessageMethod.GetAuthenticationCredentials,
                new GetAuthenticationCredentialsRequest(NuGetOrg, isRetry: false, isNonInteractive: true, canShowDialog: false),
                TestContext.Current.CancellationToken);

        var credentials = response.Should().BeOfType<GetAuthenticationCredentialsResponse>().Which;
        credentials.ResponseCode.Should().Be(MessageResponseCode.NotFound);
    }

    [Fact]
    public async Task GetAuthenticationCredentials_ForAnAzureDevOpsFeed_ReturnsTheAzureCliTokenAsABasicPassword()
    {
        using var fakeAz = FakeAz.ReturningToken("the-access-token");
        var pathWithFakeAz = fakeAz.Path + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        await using var session = await InProcessPluginSession.StartAsync(
            pathWithFakeAz,
            TestContext.Current.CancellationToken);

        var response = await session.Client
            .SendRequestAndReceiveResponseAsync<GetAuthenticationCredentialsRequest, GetAuthenticationCredentialsResponse>(
                MessageMethod.GetAuthenticationCredentials,
                new GetAuthenticationCredentialsRequest(AzureDevOpsFeed, isRetry: false, isNonInteractive: true, canShowDialog: false),
                TestContext.Current.CancellationToken);

        var credentials = response.Should().BeOfType<GetAuthenticationCredentialsResponse>().Which;
        credentials.ResponseCode.Should().Be(MessageResponseCode.Success);
        credentials.Password.Should().Be("the-access-token");
        credentials.AuthenticationTypes.Should().Contain("Basic");
    }

    [Fact]
    public async Task GetAuthenticationCredentials_WithNoAzureCli_ReturnsAnErrorNamingAzLogin()
    {
        using var withoutAz = new TempDirectory();
        await using var session = await InProcessPluginSession.StartAsync(
            withoutAz.Path,
            TestContext.Current.CancellationToken);

        var response = await session.Client
            .SendRequestAndReceiveResponseAsync<GetAuthenticationCredentialsRequest, GetAuthenticationCredentialsResponse>(
                MessageMethod.GetAuthenticationCredentials,
                new GetAuthenticationCredentialsRequest(AzureDevOpsFeed, isRetry: false, isNonInteractive: true, canShowDialog: false),
                TestContext.Current.CancellationToken);

        var credentials = response.Should().BeOfType<GetAuthenticationCredentialsResponse>().Which;
        credentials.ResponseCode.Should().Be(MessageResponseCode.Error);
        credentials.Message.Should().Contain("az login");
    }
}
