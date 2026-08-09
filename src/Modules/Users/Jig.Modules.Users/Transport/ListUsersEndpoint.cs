using FastEndpoints;
using Jig.Modules.Users.Application;

namespace Jig.Modules.Users.Transport;

internal sealed class ListUsersEndpoint(UserService users)
    : EndpointWithoutRequest<IEnumerable<UserResponse>>
{
    public override void Configure()
    {
        Get("/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await users.List(ct);
        await Send.OkAsync(result.Value!.Select(u => u.ToResponse()), ct);
    }
}
