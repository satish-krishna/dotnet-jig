using Jig.Modules.Notifications.Domain;
using Jig.Modules.Users.Contracts;
using Jig.SharedKernel;
using Microsoft.Extensions.Options;

namespace Jig.Modules.Notifications.Application;

internal sealed class UserRegisteredHandler(INotificationStore store, IOptions<NotificationsOptions> options) : IIntegrationEventHandler<UserRegistered>
{
    public Task Handle(UserRegistered e, CancellationToken ct)
        => store.Add(new Notification(PseudoKey.New(), e.UserId, string.Format(options.Value.WelcomeMessageFormat, e.Name)), ct);
}
