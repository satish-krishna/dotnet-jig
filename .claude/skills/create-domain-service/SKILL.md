---
name: create-domain-service
description: Use when adding an application service or use case to a module in this Jig modular monolith. Scaffolds an internal service in the Application layer that returns Result, its domain types, and a unit test, following the Result envelope and the domain-shaped port.
---

# Create a domain service

Read `docs/adr/0003-result-envelope.md` and `docs/adr/0002-domain-shaped-ports.md` first.

## Steps

1. Put the domain type in the module's `Domain` folder as a record, with a `PseudoKey` id the domain mints itself (`PseudoKey.New()`), not a store-assigned id. See ADR-0002.
2. Put the service in the module's `Application` folder. It depends on the module's store port (an interface in `Domain`) and, if it raises integration events, on `IEventDispatcher`. Keep it internal.
3. Every method returns `Result<T>`. Model expected failures as `Error.NotFound`, `Error.Validation`, or `Error.Conflict`; do not throw for them. Let the database judge uniqueness (catch the store's native violation and return `Error.Conflict`) rather than a check-then-act pre-check.
4. Thread `CancellationToken` through every async call.
5. Add unit tests that drive the service against a fake store and assert the `Result`.

## The shape it emits

```csharp
internal sealed class UserService(IUserStore store, IEventDispatcher events)
{
    public async Task<Result<User>> Create(string name, string email, CancellationToken ct)
    {
        var user = new User(PseudoKey.New(), name, email);
        var result = await store.Add(user, ct);
        if (result.IsSuccess)
            await events.Publish(new UserRegistered(user.Id.Value, user.Name, user.Email), ct);
        return result;
    }
}
```

Register the service in the module's `IModule.Register`. If it depends on a scoped store, register it scoped, and the host's `ValidateScopes`/`ValidateOnBuild` will catch a captive dependency at boot.
