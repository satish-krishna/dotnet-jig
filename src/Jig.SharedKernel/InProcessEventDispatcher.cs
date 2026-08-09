using Microsoft.Extensions.DependencyInjection;

namespace Jig.SharedKernel;

public sealed class InProcessEventDispatcher(IServiceProvider services) : IEventDispatcher
{
    public async Task Publish<TEvent>(TEvent e, CancellationToken ct) where TEvent : IIntegrationEvent
    {
        foreach (var handler in services.GetServices<IIntegrationEventHandler<TEvent>>())
            await handler.Handle(e, ct);
    }
}
