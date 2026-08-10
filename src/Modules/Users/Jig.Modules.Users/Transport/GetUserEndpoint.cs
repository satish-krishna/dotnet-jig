using Jig.Modules.Users.Application;
using Jig.SharedKernel;
using Jig.Web;

namespace Jig.Modules.Users.Transport;

internal sealed class GetUserEndpoint(UserService users, ICurrentUser caller)
    : ResultEndpoint<GetUserRequest, UserResponse>
{
    public override void Configure()
    {
        Get("/users/{id}");
        Policies("users:read");
    }

    public override async Task HandleAsync(GetUserRequest req, CancellationToken ct)
    {
        // The scope gate above is declarative. This one cannot be: reading only your own record
        // depends on the id in the route, so it lives in the handler, expressed through the ambient
        // caller rather than a host-side authorization requirement (which a module cannot reach).
        var isAdmin = caller.Scopes.Contains("admin");
        var isOwner = caller.UserId is { } me && me.Value == req.Id;
        if (!isAdmin && !isOwner)
        {
            AddError("You may only read your own user.");
            await Send.ErrorsAsync(403, ct);
            return;
        }

        var result = await users.Get(new PseudoKey(req.Id), ct);
        await SendResultAsync(result, u => u.ToResponse(), ct);
    }
}
