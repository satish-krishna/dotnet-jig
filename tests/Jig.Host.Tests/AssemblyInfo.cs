using Xunit;

// These tests install process-global ActivityListener/MeterListener instances that match the
// "Jig" ActivitySource and Meter by name. Another test class emitting on its own "Jig" sources
// concurrently would pollute those listeners, so this assembly runs its collections serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
