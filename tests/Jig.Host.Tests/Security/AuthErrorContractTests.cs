using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace Jig.Host.Tests.Security;

public class AuthErrorContractTests
{
    [Fact]
    public async Task Unauthorized_is_problem_details()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/v1/users",
            new { name = "Ada", email = "ada@example.com" }, TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        res.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }
}
