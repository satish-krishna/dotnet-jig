using Jig.SharedKernel;

namespace Jig.Modules.Users.Domain;

internal interface IUserStore
{
    Task<IReadOnlyList<User>> All(CancellationToken ct);
    Task<User?> Find(PseudoKey id, CancellationToken ct);
    Task<User?> FindByEmail(string email, CancellationToken ct);
    Task Add(User user, CancellationToken ct);
}
