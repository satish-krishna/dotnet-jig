using Jig.Modules.Notifications.Domain;
using Jig.Modules.Users.Contracts;
using Jig.SharedKernel;

namespace Jig.Modules.Notifications.Application;

internal sealed class UserRegisteredHandler(INotificationStore store) : IIntegrationEventHandler<UserRegistered>
{
    public Task Handle(UserRegistered e, CancellationToken ct)
        => store.Add(new Notification(PseudoKey.New(), e.UserId, $"Welcome, {e.Name}!"), ct);
}
