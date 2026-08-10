using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;

namespace Jig.Host.Tests.Security;

public class AuthorizationTests
{
    [Fact]
    public async Task Create_without_write_scope_is_forbidden()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person(Guid.NewGuid().ToString(), "users:read"));

        var res = await client.PostAsJsonAsync("/v1/users",
            new { name = "Ada", email = "ada@example.com" }, TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        res.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }
}
