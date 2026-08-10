---
name: create-endpoint
description: Use when adding an HTTP endpoint to a module in this Jig modular monolith. Scaffolds a FastEndpoints endpoint in the module's Transport layer, wired to an application service, returning a Result and mapped by ResultEndpoint, with a declarative scope policy.
---

# Create an endpoint

Read `docs/adr/0003-result-envelope.md` and `docs/adr/0005-ambient-caller.md` first.

## Steps

1. Confirm the target module and the application service the endpoint calls. If the service does not exist yet, use the `create-domain-service` skill first.
2. Create the endpoint in `src/Modules/<Module>/Jig.Modules.<Module>/Transport/`. Use `ResultEndpoint<TRequest, TResponse>` so the `Result` envelope maps to an HTTP status in one place; do not write status codes or `if` checks for expected failures in the handler.
3. Declare authorization at the endpoint with `Policies("<scope>")`, never as an `if` in the handler. FastEndpoints requires an authenticated caller by default, so add `AllowAnonymous()` only for a genuinely public endpoint (health, a dev-only route). For a rule that depends on the resource (owner-only), read the caller through `ICurrentUser` and return a modeled 403; that one cannot be a static policy.
4. Add a `Request` and `Response` record in the module's `Transport`. The `Response` is a wire type, distinct from the domain type.
5. Add a test in the module's test project that drives the endpoint through `WebApplicationFactory` and asserts the status and body.

## The shape it emits

```csharp
internal sealed class CreateUserEndpoint(UserService users)
    : ResultEndpoint<CreateUserRequest, UserResponse>
{
    public override void Configure()
    {
        Post("/users");
        Policies("users:write");
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        var result = await users.Create(req.Name, req.Email, ct);
        await SendResultAsync(result, u => u.ToResponse(), ct);
    }
}
```

Then run `dotnet test`, and `bun run codegen` so the generated client picks up the new endpoint.
