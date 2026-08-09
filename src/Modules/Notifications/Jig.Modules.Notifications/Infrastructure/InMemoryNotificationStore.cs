using System.Collections.Concurrent;
using Jig.Modules.Notifications.Domain;

namespace Jig.Modules.Notifications.Infrastructure;

internal sealed class InMemoryNotificationStore : INotificationStore
{
    private readonly ConcurrentBag<Notification> _notes = new();

    public Task Add(Notification note, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _notes.Add(note);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Notification>> ForUser(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<Notification> r = _notes.Where(n => n.UserId == userId).ToArray();
        return Task.FromResult(r);
    }
}
