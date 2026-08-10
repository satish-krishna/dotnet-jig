using FastEndpoints;
using Jig.Modules.Notifications.Domain;
using Jig.Modules.Users.Contracts;
using Jig.SharedKernel;

namespace Jig.Modules.Notifications.Transport;

internal sealed class ListUserNotificationsEndpoint(INotificationStore store, IUserDirectory users, ICurrentUser caller)
    : EndpointWithoutRequest<UserNotificationsResponse>
{
    public override void Configure()
    {
        Get("/users/{id}/notifications");
        Policies("users:read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");

        // Notifications are per-user data, so the same resource rule as reading the user applies:
        // your own notifications, or admin. Expressed through the ambient caller, in the module.
        var isAdmin = caller.Scopes.Contains("admin");
        var isOwner = caller.UserId is { } me && me.Value == id;
        if (!isAdmin && !isOwner)
        {
            AddError("You may only read your own notifications.");
            await Send.ErrorsAsync(403, ct);
            return;
        }

        var who = await users.GetById(id, ct);
        var name = who.IsSuccess ? who.Value!.Name : "unknown user";
        var notes = await store.ForUser(id, ct);
        await Send.OkAsync(new UserNotificationsResponse(name, notes.Select(n => n.Message).ToArray()), ct);
    }
}
