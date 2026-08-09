using Jig.Modules.Users.Domain;
using Jig.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Jig.Modules.Users.Infrastructure;

/// <summary>A decorator over whichever provider store won registration (EF or Mongo). It takes
/// the interface it decorates as its first dependency and logs each call before delegating, so
/// swapping providers never means swapping logging back in.
///
/// Deliberately boring: it logs, it does not cache. A caching decorator over a store that also
/// writes needs an invalidation story, and a template that ships a subtly stale cache is worse
/// than one that ships a log line.</summary>
internal sealed class LoggingUserStore(IUserStore inner, ILogger<LoggingUserStore> log) : IUserStore
{
    public async Task<IReadOnlyList<User>> All(CancellationToken ct)
    {
        log.LogInformation("Listing all users");
        return await inner.All(ct);
    }

    public async Task<User?> Find(PseudoKey id, CancellationToken ct)
    {
        log.LogInformation("Finding user {UserId}", id);
        return await inner.Find(id, ct);
    }

    public async Task<User?> FindByEmail(string email, CancellationToken ct)
    {
        log.LogInformation("Finding user by email {Email}", email);
        return await inner.FindByEmail(email, ct);
    }

    public async Task<Result<User>> Add(User user, CancellationToken ct)
    {
        var result = await inner.Add(user, ct);

        if (result.IsSuccess)
            log.LogInformation("User {UserId} added", user.Id);
        else
            log.LogInformation("User insert rejected: {Code}", result.Error!.Code);

        return result;
    }
}
