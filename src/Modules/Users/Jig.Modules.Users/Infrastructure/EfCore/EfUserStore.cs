using Jig.Modules.Users.Domain;
using Jig.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Jig.Modules.Users.Infrastructure.EfCore;

/// <summary>The EF Core side of the <see cref="IUserStore"/> seam. Everything provider-specific
/// stops here: the LINQ and the DbContext. The Application layer gets domain types and nothing
/// else.</summary>
internal sealed class EfUserStore(JigDbContext db) : IUserStore
{
    public async Task<IReadOnlyList<User>> All(CancellationToken ct)
        => await db.Users.AsNoTracking().ToListAsync(ct);

    public async Task<User?> Find(PseudoKey id, CancellationToken ct)
        => await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> FindByEmail(string email, CancellationToken ct)
        // The Email column is TEXT COLLATE NOCASE, so this equality comparison is already
        // case-insensitive at the database: "Ada@x.com" and "ada@x.com" match the same row.
        => await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email, ct);

    public async Task Add(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}
