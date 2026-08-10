using FastEndpoints;
using Jig.Modules.Users.Application;
using Jig.SharedKernel;
using Jig.Web;

namespace Jig.Modules.Users.Transport;

// Proves the ambient caller flows into a module: it reads ICurrentUser.UserId (no HttpContext in
// sight) and returns that user. Authenticated by default; no specific scope required.
internal sealed class MeEndpoint(UserService users, ICurrentUser caller)
    : ResultEndpoint<EmptyRequest, UserResponse>
{
    public override void Configure() => Get("/me");

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        if (caller.UserId is not { } id)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = await users.Get(id, ct);
        await SendResultAsync(result, u => u.ToResponse(), ct);
    }
}
