using Jig.Modules.Users.Application;
using Jig.Modules.Users.Domain;
using Jig.Modules.Users.Infrastructure;
using Jig.SharedKernel;
using Shouldly;
using Xunit;

namespace Jig.Modules.Users.Tests;

public class UserServiceGetTests
{
    private static UserService NewService(IUserStore store) => new(store, new NoOpDispatcher());

    [Fact]
    public async Task Get_returns_the_user_when_present()
    {
        var store = new InMemoryUserStore();
        var user = new User(PseudoKey.New(), "Ada", "ada@x.com");
        await store.Add(user, TestContext.Current.CancellationToken);

        var result = await NewService(store).Get(user.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Email.ShouldBe("ada@x.com");
    }

    [Fact]
    public async Task Get_is_not_found_when_absent()
    {
        var result = await NewService(new InMemoryUserStore()).Get(PseudoKey.New(), TestContext.Current.CancellationToken);
        result.Error!.Kind.ShouldBe(ErrorKind.NotFound);
    }
}
