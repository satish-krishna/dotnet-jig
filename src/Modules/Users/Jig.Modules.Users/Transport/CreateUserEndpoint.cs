using Jig.Modules.Users.Application;
using Jig.Web;

namespace Jig.Modules.Users.Transport;

internal sealed class CreateUserEndpoint(UserService users)
    : ResultEndpoint<CreateUserRequest, UserResponse>
{
    public override void Configure()
    {
        Post("/users");
        Policies("users:write");
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        var result = await users.Create(req.Name, req.Email, ct);
        await SendResultAsync(result, u => u.ToResponse(), ct);
    }
}
