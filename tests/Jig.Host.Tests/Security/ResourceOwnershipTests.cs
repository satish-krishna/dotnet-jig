using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;

namespace Jig.Host.Tests.Security;

public class ResourceOwnershipTests
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
    public async Task Reading_another_users_record_is_forbidden_without_admin()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        var id = await CreateUser(client, "owner@example.com", TestContext.Current.CancellationToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person(Guid.NewGuid().ToString(), "users:read"));
        var res = await client.GetAsync($"/v1/users/{id}", TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        res.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Reading_your_own_record_is_allowed()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        var id = await CreateUser(client, "self@example.com", TestContext.Current.CancellationToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person(id, "users:read"));
        var res = await client.GetAsync($"/v1/users/{id}", TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_can_read_any_record()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        var id = await CreateUser(client, "someone@example.com", TestContext.Current.CancellationToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", DevTokens.Person(Guid.NewGuid().ToString(), "users:read", "admin"));
        var res = await client.GetAsync($"/v1/users/{id}", TestContext.Current.CancellationToken);

        res.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
