using Jig.Modules.Users.Contracts;
using Jig.Modules.Users.Domain;
using Jig.SharedKernel;

namespace Jig.Modules.Users.Application;

internal sealed class UserDirectory(IUserStore store) : IUserDirectory
{
    public async Task<Result<UserSummary>> GetById(Guid id, CancellationToken ct)
    {
        var user = await store.Find(new PseudoKey(id), ct);
        return user is null
            ? Error.NotFound($"User {id} was not found.")
            : new UserSummary(user.Id.ToString(), user.Name, user.Email);
    }
}
