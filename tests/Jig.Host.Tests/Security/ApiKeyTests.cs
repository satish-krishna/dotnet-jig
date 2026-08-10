using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace Jig.Host.Tests.Security;

public class ApiKeyTests
{
    [Fact]
    public async Task Api_key_with_write_scope_passes_the_same_policy()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-machine-key");

        var res = await client.PostAsJsonAsync("/v1/users",
            new { name = "Bot", email = "bot@example.com" }, TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unknown_api_key_is_unauthorized()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "not-a-real-key");

        var res = await client.PostAsJsonAsync("/v1/users",
            new { name = "Bot", email = "bot2@example.com" }, TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
