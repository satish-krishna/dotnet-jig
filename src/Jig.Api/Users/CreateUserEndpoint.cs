using FastEndpoints;
using Jig.Application;

namespace Jig.Api.Users;

/// <summary>POST /users. Creates a user. Validation is a later decision; this takes
/// the request at face value for now.</summary>
public sealed class CreateUserEndpoint : Endpoint<CreateUserRequest, UserResponse>
{
    private readonly UserService _users;

    public CreateUserEndpoint(UserService users) => _users = users;

    public override void Configure()
    {
        Post("/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        var user = _users.Create(req.Name, req.Email);
        await Send.OkAsync(user.ToResponse(), ct);
    }
}
