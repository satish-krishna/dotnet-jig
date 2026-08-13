using Jig.Modules.Users.Domain;
using Jig.SharedKernel;
using MongoDB.Driver;

namespace Jig.Modules.Users.Infrastructure.Mongo;

/// <summary>The other side of the same seam. No DbContext, no expression trees translated into
/// SQL, and nothing above this class knows the difference: the Application layer and every
/// use-case test are compiled against IUserStore and go untouched by this class existing.
///
/// The shape matches EfUserStore on purpose. The store enforces uniqueness with its own index,
/// catches its own driver exception, and returns the same domain Conflict, so the check-then-act
/// race in the use-case is closed the same way in both worlds.</summary>
internal sealed class MongoUserStore(IMongoCollection<UserDocument> users) : IUserStore
{
    /// <summary>The uniqueness rule, enforced by the database rather than only by a check in the
    /// use-case. NormalizedEmail is already folded by the domain, so this is a plain unique
    /// index: no collation is needed, and this index is what makes the check-then-act race
    /// harmless, the second writer loses here. Called once at startup (or by a test before it
    /// starts asserting), not per-request.</summary>
    public static async Task EnsureIndexes(IMongoCollection<UserDocument> collection, CancellationToken ct)
    {
        var keys = Builders<UserDocument>.IndexKeys.Ascending(u => u.NormalizedEmail);
        var options = new CreateIndexOptions<UserDocument> { Unique = true };

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<UserDocument>(keys, options), cancellationToken: ct);
    }

    public async Task<IReadOnlyList<User>> All(CancellationToken ct)
    {
        var documents = await users
            .Find(FilterDefinition<UserDocument>.Empty)
            .SortBy(u => u.Id)
            .ToListAsync(ct);

        return documents.Select(d => d.ToDomain()).ToArray();
    }

    public async Task<User?> Find(PseudoKey id, CancellationToken ct)
    {
        var document = await users.Find(u => u.Id == id.Value).SingleOrDefaultAsync(ct);
        return document?.ToDomain();
    }

    public async Task<User?> FindByEmail(string email, CancellationToken ct)
    {
        // The domain folds email once; the filter is against that already-normalized value,
        // so no collation is needed for Mongo's default binary comparison to match
        // "Ada@x.com" when asked for "ada@x.com".
        var normalized = User.Normalize(email);
        var document = await users.Find(u => u.NormalizedEmail == normalized).SingleOrDefaultAsync(ct);
        return document?.ToDomain();
    }

    public async Task<Result<User>> Add(User user, CancellationToken ct)
    {
        // Note what is not here: no id generation. The domain issued the pseudo key before this
        // method was called, so the insert is an insert. This is the whole method, and it is the
        // same length as the relational one.
        try
        {
            await users.InsertOneAsync(UserDocument.From(user), cancellationToken: ct);
            return Result<User>.Success(user);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Somebody else took the email between the use-case's check and this insert. The
            // unique index caught it, and the driver's exception stops here rather than crossing
            // the seam.
            return Result<User>.Failure(Error.Conflict($"Email {user.Email} is already in use."));
        }
    }
}
