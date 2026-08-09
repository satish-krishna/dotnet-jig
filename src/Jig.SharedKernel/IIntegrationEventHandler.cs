namespace Jig.SharedKernel;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task Handle(TEvent e, CancellationToken ct);
}
