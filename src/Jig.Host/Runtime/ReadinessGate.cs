namespace Jig.Host.Runtime;

// A single latch the readiness probe reads. It starts open and is closed once, on shutdown,
// so an orchestrator takes the instance out of rotation while in-flight work drains (Task 6).
// Liveness is deliberately separate: the process is still alive and must not be killed.
internal sealed class ReadinessGate
{
    private volatile bool _ready = true;

    public bool IsReady => _ready;

    public void MarkNotReady() => _ready = false;
}
