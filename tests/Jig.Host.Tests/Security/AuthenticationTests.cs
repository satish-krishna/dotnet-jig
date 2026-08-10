using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;

namespace Jig.Host.Tests.Security;

public class AuthenticationTests
{
    [Fact]
    public async Task Anonymous_create_is_rejected()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/v1/users",
            new { name = "Ada", email = "ada@example.com" }, TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_create_succeeds()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person("11111111-1111-1111-1111-111111111111", "users:write"));

        var res = await client.PostAsJsonAsync("/v1/users",
            new { name = "Ada", email = "ada@example.com" }, TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
