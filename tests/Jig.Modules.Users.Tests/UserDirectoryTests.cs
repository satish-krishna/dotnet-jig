using Jig.Modules.Users.Application;
using Jig.Modules.Users.Domain;
using Jig.Modules.Users.Infrastructure;
using Jig.SharedKernel;
using Shouldly;
using Xunit;

namespace Jig.Modules.Users.Tests;

public class UserDirectoryTests
{
    [Fact]
    public async Task GetById_returns_a_summary_for_a_known_user()
    {
        var store = new InMemoryUserStore();
        var user = new User(PseudoKey.New(), "Ada", "ada@x.com");
        await store.Add(user, TestContext.Current.CancellationToken);
        var directory = new UserDirectory(store);

        var found = await directory.GetById(user.Id.Value, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value!.Email.ShouldBe("ada@x.com");
    }

    [Fact]
    public async Task GetById_is_not_found_for_an_unknown_id()
    {
        var directory = new UserDirectory(new InMemoryUserStore());
        var found = await directory.GetById(Guid.NewGuid(), TestContext.Current.CancellationToken);
        found.Error!.Kind.ShouldBe(ErrorKind.NotFound);
    }
}
