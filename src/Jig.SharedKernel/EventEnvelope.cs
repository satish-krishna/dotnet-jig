using System.Diagnostics;

namespace Jig.SharedKernel;

// An integration event captured for off-thread dispatch. ParentContext is the trace
// context at publish time, so the worker can link its span back to the request that
// raised the event. Dispatch closes over the generic event type, so the worker can run
// the right IIntegrationEventHandler<TEvent> without reflecting over the payload.
public sealed record EventEnvelope(
    string EventType,
    ActivityContext ParentContext,
    Func<IServiceProvider, CancellationToken, Task> Dispatch);
