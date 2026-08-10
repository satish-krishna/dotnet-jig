using System.Diagnostics;
using System.Threading.Channels;

namespace Jig.SharedKernel;

// Publishing no longer runs handlers on the caller's thread. It captures the event and the
// current trace context into an envelope and hands it to the channel; the EventPump drains
// the channel and runs the handlers off the request thread, one scope per event. The closure
// reuses InProcessEventDispatcher so the handler-resolution rule stays in one place.
public sealed class ChannelEventDispatcher(Channel<EventEnvelope> channel) : IEventDispatcher
{
    public async Task Publish<TEvent>(TEvent e, CancellationToken ct) where TEvent : IIntegrationEvent
    {
        var envelope = new EventEnvelope(
            typeof(TEvent).Name,
            Activity.Current?.Context ?? default,
            (sp, c) => new InProcessEventDispatcher(sp).Publish(e, c));

        await channel.Writer.WriteAsync(envelope, ct);
    }
}
