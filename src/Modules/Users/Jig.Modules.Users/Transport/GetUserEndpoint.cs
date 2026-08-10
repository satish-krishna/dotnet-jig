using Jig.Modules.Users.Application;
using Jig.SharedKernel;
using Jig.Web;

namespace Jig.Modules.Users.Transport;

internal sealed class GetUserEndpoint(UserService users)
    : ResultEndpoint<GetUserRequest, UserResponse>
{
    public override void Configure()
    {
        Get("/users/{id}");
        Policies("users:read");
    }

    public override async Task HandleAsync(GetUserRequest req, CancellationToken ct)
    {
        var result = await users.Get(new PseudoKey(req.Id), ct);
        await SendResultAsync(result, u => u.ToResponse(), ct);
    }
}
