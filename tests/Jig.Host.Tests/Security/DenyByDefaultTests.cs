using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;

namespace Jig.Host.Tests.Security;

// Deny-by-default is a property of the whole monolith, not one module. These guard the endpoints
// beyond the Users create/get/me path: the Notifications module's per-user route, and the list route
// that must not be a way around per-record ownership.
public class DenyByDefaultTests
{
    private sealed record UserDto(string Id, string Name, string Email);

    private static async Task<string> CreateUser(HttpClient client, string email, CancellationToken ct)
    {
        client.DefaultRequestHeaders.Remove("X-Api-Key");
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-machine-key");
        var created = await client.PostAsJsonAsync("/v1/users", new { name = "Ada", email }, ct);
        created.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await created.Content.ReadFromJsonAsync<UserDto>(ct);
        client.DefaultRequestHeaders.Remove("X-Api-Key");
        return user!.Id;
    }

    [Fact]
    public async Task Notifications_are_not_readable_anonymously()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        var id = await CreateUser(client, "n-anon@example.com", TestContext.Current.CancellationToken);

        var res = await client.GetAsync($"/v1/users/{id}/notifications", TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reading_another_users_notifications_is_forbidden()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        var id = await CreateUser(client, "n-owner@example.com", TestContext.Current.CancellationToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person(Guid.NewGuid().ToString(), "users:read"));
        var res = await client.GetAsync($"/v1/users/{id}/notifications", TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reading_your_own_notifications_is_allowed()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        var id = await CreateUser(client, "n-self@example.com", TestContext.Current.CancellationToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person(id, "users:read"));
        var res = await client.GetAsync($"/v1/users/{id}/notifications", TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Listing_all_users_requires_admin_not_just_read()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person(Guid.NewGuid().ToString(), "users:read"));
        var forbidden = await client.GetAsync("/v1/users", TestContext.Current.CancellationToken);
        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person(Guid.NewGuid().ToString(), "admin"));
        var allowed = await client.GetAsync("/v1/users", TestContext.Current.CancellationToken);
        allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
