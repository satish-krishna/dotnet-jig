using System.Collections.Concurrent;
using Jig.Domain;

namespace Jig.Application;

/// <summary>The users use-case. It holds the store in memory for now: where users
/// actually live is the Persistence decision's job, on its own branch later. Nothing
/// here knows about a database, and that is the point.</summary>
public sealed class UserService
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextId;

    public IReadOnlyCollection<User> All() => _users.Values.ToArray();

    public User? Find(int id) => _users.TryGetValue(id, out var user) ? user : null;

    public User Create(string name, string email)
    {
        var id = Interlocked.Increment(ref _nextId);
        var user = new User(id, name, email);
        _users[id] = user;
        return user;
    }
}
