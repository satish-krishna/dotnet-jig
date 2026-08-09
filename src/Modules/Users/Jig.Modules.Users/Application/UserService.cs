using Jig.Modules.Users.Contracts;
using Jig.Modules.Users.Domain;
using Jig.SharedKernel;

namespace Jig.Modules.Users.Application;

internal sealed class UserService(IUserStore store, IEventDispatcher events)
{
    public async Task<Result<IReadOnlyList<User>>> List(CancellationToken ct)
        => Result<IReadOnlyList<User>>.Success(await store.All(ct));

    public async Task<Result<User>> Create(string name, string email, CancellationToken ct)
    {
        if (await store.FindByEmail(email, ct) is not null)
            return Error.Conflict($"Email {email} is already in use.");

        var user = new User(PseudoKey.New(), name, email);
        await store.Add(user, ct);
        await events.Publish(new UserRegistered(user.Id.Value, user.Name, user.Email), ct);
        return user;
    }
}
