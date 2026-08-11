using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace Jig.Host.Tests;

public class VersionEndpointTests
{
    [Fact]
    public async Task Version_returns_the_running_build()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/version", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<VersionInfo>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        // The SDK bakes SourceRevisionId from .git even on a plain build, so a working-tree build
        // reports the real HEAD; a Docker build feeds it GIT_SHA instead, and an un-stamped build
        // reads back "unknown". Any of those is a non-empty answer; a blank one means broken wiring.
        body.Version.ShouldNotBeNullOrWhiteSpace();
        body.Sha.ShouldNotBeNullOrWhiteSpace();
    }
}
