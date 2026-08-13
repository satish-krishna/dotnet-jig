using Jig.SharedKernel;

namespace Jig.Modules.Users.Domain;

internal record User(PseudoKey Id, string Name, string Email)
{
    // The single fold. Uniqueness is decided on this, in the domain, so two
    // databases never get to disagree about what "the same email" means.
    public string NormalizedEmail { get; init; } = Normalize(Email);

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
