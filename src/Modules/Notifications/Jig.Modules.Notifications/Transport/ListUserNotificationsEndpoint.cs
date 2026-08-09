using FastEndpoints;
using Jig.Modules.Notifications.Domain;
using Jig.Modules.Users.Contracts;

namespace Jig.Modules.Notifications.Transport;

internal sealed class ListUserNotificationsEndpoint(INotificationStore store, IUserDirectory users)
    : EndpointWithoutRequest<UserNotificationsResponse>
{
    public override void Configure()
    {
        Get("/users/{id}/notifications");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var who = await users.GetById(id, ct);
        var name = who.IsSuccess ? who.Value!.Name : "unknown user";
        var notes = await store.ForUser(id, ct);
        await Send.OkAsync(new UserNotificationsResponse(name, notes.Select(n => n.Message).ToArray()), ct);
    }
}
