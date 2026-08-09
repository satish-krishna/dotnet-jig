using Jig.Modules.Users.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jig.Modules.Users.Tests;

public class EfUserStoreTests
{
    [Fact]
    public async Task Honors_the_store_contract()
    {
        var options = new DbContextOptionsBuilder<JigDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db")}")
            .Options;
        await using var db = new JigDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await UserStoreContract.Run(new EfUserStore(db), TestContext.Current.CancellationToken);
    }
}
