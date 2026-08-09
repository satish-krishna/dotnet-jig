using System.Collections.Concurrent;
using Jig.Modules.Users.Domain;
using Jig.SharedKernel;

namespace Jig.Modules.Users.Infrastructure;

internal sealed class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<PseudoKey, User> _users = new();

    public Task<IReadOnlyList<User>> All(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<User> all = _users.Values.ToArray();
        return Task.FromResult(all);
    }

    public Task<User?> Find(PseudoKey id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> FindByEmail(string email, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = _users.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<Result<User>> Add(User user, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Mirrors the database's case-insensitive unique index on Email: the in-memory store is
        // the single arbiter of uniqueness here too, not a pre-check the caller has to run first.
        if (_users.Values.Any(u => string.Equals(u.Email, user.Email, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(Result<User>.Failure(Error.Conflict($"Email {user.Email} is already in use.")));

        _users[user.Id] = user;
        return Task.FromResult(Result<User>.Success(user));
    }
}
