using System.Net;
using System.Net.Http.Headers;
using Shouldly;

namespace Jig.Host.Tests.Security;

public class CurrentUserTests
{
    [Fact]
    public async Task Me_resolves_the_token_subject_then_404_when_no_row_exists()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person(Guid.NewGuid().ToString(), "users:read"));

        var res = await client.GetAsync("/v1/me", TestContext.Current.CancellationToken);

        // The subject was read from the token (so not 401); there is just no user row for it yet.
        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Me_is_rejected_when_anonymous()
    {
        await using var factory = new JigApiFactory();

        var res = await factory.CreateClient().GetAsync("/v1/me", TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
