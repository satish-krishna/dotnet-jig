namespace Jig.Domain;

/// <summary>The user, as the domain understands it. No framework, no persistence,
/// no wire concerns. Domain references nothing, so it cannot depend on any of them.</summary>
public record User(int Id, string Name, string Email);
