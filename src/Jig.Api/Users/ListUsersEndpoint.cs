using FastEndpoints;
using Jig.Application;

namespace Jig.Api.Users;

/// <summary>GET /users. Returns every user.</summary>
public sealed class ListUsersEndpoint : EndpointWithoutRequest<IEnumerable<UserResponse>>
{
    private readonly UserService _users;

    public ListUsersEndpoint(UserService users) => _users = users;

    public override void Configure()
    {
        Get("/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => await Send.OkAsync(_users.All().Select(u => u.ToResponse()), ct);
}
