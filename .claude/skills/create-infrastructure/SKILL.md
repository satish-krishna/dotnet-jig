---
name: create-infrastructure
description: Use when adding persistence or another external adapter to a module in this Jig modular monolith. Scaffolds a domain-shaped store port and a provider implementation behind it, following per-module store ownership and the runtime provider choice.
---

# Create infrastructure

Read `docs/adr/0002-domain-shaped-ports.md` and `docs/adr/0004-per-module-store-ownership.md` first.

## Steps

1. Define the port in the module's `Domain` as an interface shaped by what the module does with its data, not a generic `IRepository<T>`. Give it only the methods the module needs, and return `Result<T>` where a call can fail in a modeled way (a duplicate becomes `Error.Conflict`).
2. Implement it in the module's `Infrastructure`, one implementation per provider (EF Core, Mongo). Keep every provider-specific type (the `DbContext`, a storage document) inside `Infrastructure`; map to and from the domain type at the boundary so the store's concerns never cross the seam.
3. Select the provider at runtime from configuration inside a factory, not with a compile-time reference, so the same build runs on either store. Decorate the chosen store once at the seam (for example a logging decorator) rather than per provider.
4. Let the database enforce invariants (a unique index) and translate the native violation into a modeled `Result`, so a race returns a clean `Conflict` rather than a 500.
5. A real store is scoped (a `DbContext`, request-bound handles), so register the port scoped and let the container's boot-time validation catch a captive dependency.
6. Prove both providers against one shared contract test, so a swap you have not run is not a swap you are guessing about.

## The shape it emits

```csharp
// Domain/IUserStore.cs
internal interface IUserStore
{
    Task<User?> Find(PseudoKey id, CancellationToken ct);
    Task<Result<User>> Add(User user, CancellationToken ct);
}

// Infrastructure/EfCore/EfUserStore.cs
internal sealed class EfUserStore(JigDbContext db) : IUserStore
{
    public async Task<Result<User>> Add(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        try { await db.SaveChangesAsync(ct); return user; }
        catch (DbUpdateException) { return Error.Conflict($"Email {user.Email} is already in use."); }
    }
    // Find and the rest follow the port.
}
```

Register the port, the provider implementations, and the selecting factory in the module's `IModule.Register`, and run the shared contract test against every provider.
