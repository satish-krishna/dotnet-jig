using Jig.Modules.Users.Contracts;
using Jig.Modules.Users.Domain;
using Jig.SharedKernel;

namespace Jig.Modules.Users.Application;

internal sealed class UserService(IUserStore store, IEventDispatcher events)
{
    public async Task<Result<IReadOnlyList<User>>> List(CancellationToken ct)
        => Result<IReadOnlyList<User>>.Success(await store.All(ct));

    public async Task<Result<User>> Get(PseudoKey id, CancellationToken ct)
    {
        var user = await store.Find(id, ct);
        return user is null ? Error.NotFound($"User {id} was not found.") : user;
    }

    public async Task<Result<User>> Create(string name, string email, CancellationToken ct)
    {
        var user = new User(PseudoKey.New(), name, email);
        var result = await store.Add(user, ct);
        if (result.IsSuccess)
            await events.Publish(new UserRegistered(user.Id.Value, user.Name, user.Email), ct);
        return result;
    }
}
