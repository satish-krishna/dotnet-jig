using Jig.Modules.Notifications.Application;
using Jig.Modules.Notifications.Domain;
using Jig.Modules.Notifications.Infrastructure;
using Jig.Modules.Users.Contracts;
using Jig.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Jig.Modules.Notifications;

internal sealed class NotificationsModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<INotificationStore, InMemoryNotificationStore>();
        services.AddSingleton<IIntegrationEventHandler<UserRegistered>, UserRegisteredHandler>();
    }
}
