using FastEndpoints;
using Jig.Application;

namespace Jig.Api.Users;

/// <summary>GET /users/{id}. Returns one user, or 404.</summary>
public sealed class GetUserEndpoint : Endpoint<GetUserRequest, UserResponse>
{
    private readonly UserService _users;

    public GetUserEndpoint(UserService users) => _users = users;

    public override void Configure()
    {
        Get("/users/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetUserRequest req, CancellationToken ct)
    {
        var user = _users.Find(req.Id);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(user.ToResponse(), ct);
    }
}
