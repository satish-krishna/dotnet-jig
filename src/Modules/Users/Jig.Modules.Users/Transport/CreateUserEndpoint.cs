using FastEndpoints;
using Jig.Modules.Users.Application;
using Microsoft.AspNetCore.Http;

namespace Jig.Modules.Users.Transport;

internal sealed class CreateUserEndpoint(UserService users)
    : Endpoint<CreateUserRequest, UserResponse>
{
    public override void Configure()
    {
        Post("/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        var result = await users.Create(req.Name, req.Email, ct);
        if (!result.IsSuccess)
        {
            await Send.ResultAsync(Results.Conflict(result.Error!.Message));
            return;
        }
        await Send.OkAsync(result.Value!.ToResponse(), ct);
    }
}
