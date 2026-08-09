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
        var user = _users.Values.FirstOrDefault(u => u.Email == email);
        return Task.FromResult(user);
    }

    public Task Add(User user, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _users[user.Id] = user;
        return Task.CompletedTask;
    }
}
