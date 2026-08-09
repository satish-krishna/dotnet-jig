namespace Jig.SharedKernel;

public interface IEventDispatcher
{
    Task Publish<TEvent>(TEvent e, CancellationToken ct) where TEvent : IIntegrationEvent;
}
