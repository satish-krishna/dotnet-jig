using Microsoft.Extensions.DependencyInjection;

namespace Jig.SharedKernel;

public sealed class InProcessEventDispatcher(IServiceProvider services) : IEventDispatcher
{
    public async Task Publish<TEvent>(TEvent e, CancellationToken ct) where TEvent : IIntegrationEvent
    {
        // `services` here is the root IServiceProvider, so this resolves handlers as
        // singletons today (Risk R2, see UsersModule.cs). That is correct only while every
        // handler/store stays singleton. Once a module's store and handler flip to scoped on
        // the persistence branch, resolving them off the root provider will fail scope
        // validation or capture the wrong scope. When that happens, this dispatch must create
        // an IServiceScope per publish and resolve handlers from it instead.
        foreach (var handler in services.GetServices<IIntegrationEventHandler<TEvent>>())
            await handler.Handle(e, ct);
    }
}
