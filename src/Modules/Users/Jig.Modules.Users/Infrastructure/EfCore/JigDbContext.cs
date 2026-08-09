using Jig.Modules.Users.Domain;
using Jig.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Jig.Modules.Users.Infrastructure.EfCore;

/// <summary>The EF Core context for the Users module. It lives in Infrastructure and is never
/// injected anywhere else: the only type that touches it is <see cref="EfUserStore"/> in this
/// same folder. That is what stops the query language from wandering into the Application
/// layer.</summary>
internal sealed class JigDbContext(DbContextOptions<JigDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();

        // PseudoKey is a domain-issued Guid, not a store-generated identity: the store must
        // never invent it, so the conversion below is the only thing standing between the
        // struct and the raw column, and ValueGeneratedNever keeps EF from assigning one.
        user.Property(u => u.Id)
            .HasConversion(id => id.Value, value => new PseudoKey(value))
            .ValueGeneratedNever();

        user.HasKey(u => u.Id);

        user.Property(u => u.Name).IsRequired().HasMaxLength(200);

        user.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320)
            .HasColumnType("TEXT COLLATE NOCASE");

        // The uniqueness rule, enforced by the database rather than only by the check in the
        // use-case. NOCASE makes "Ada@x.com" and "ada@x.com" collide, so the index is what
        // makes the check-then-act race harmless: the second writer loses here.
        user.HasIndex(u => u.Email).IsUnique();
    }
}
