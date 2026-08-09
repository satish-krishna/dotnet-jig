namespace Jig.Modules.Notifications.Domain;

internal interface INotificationStore
{
    Task Add(Notification note, CancellationToken ct);
    Task<IReadOnlyList<Notification>> ForUser(Guid userId, CancellationToken ct);
}
