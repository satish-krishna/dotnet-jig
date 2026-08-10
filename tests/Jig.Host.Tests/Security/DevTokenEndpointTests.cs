using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;

namespace Jig.Host.Tests.Security;

public class DevTokenEndpointTests
{
    private sealed record TokenResponse(string Token);

    [Fact]
    public async Task Dev_token_endpoint_is_absent_outside_development()
    {
        await using var factory = new JigApiFactory(); // Production
        var res = await factory.CreateClient().PostAsJsonAsync("/dev/token",
            new { subject = Guid.NewGuid().ToString(), scopes = new[] { "users:read" } },
            TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Dev_token_endpoint_mints_a_usable_token_in_development()
    {
        await using var factory = new JigApiFactory("Development");
        var client = factory.CreateClient();

        var mint = await client.PostAsJsonAsync("/dev/token",
            new { subject = Guid.NewGuid().ToString(), scopes = new[] { "users:write" } },
            TestContext.Current.CancellationToken);
        mint.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await mint.Content.ReadFromJsonAsync<TokenResponse>(TestContext.Current.CancellationToken);
        body!.Token.ShouldNotBeNullOrWhiteSpace();

        // The minted token actually clears a protected endpoint.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        var create = await client.PostAsJsonAsync("/v1/users",
            new { name = "Ada", email = "dev@example.com" }, TestContext.Current.CancellationToken);

        create.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
