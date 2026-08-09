using Jig.Modules.Users.Application;
using Jig.Modules.Users.Infrastructure;
using Jig.SharedKernel;
using Shouldly;
using Xunit;

namespace Jig.Modules.Users.Tests;

internal sealed class NoOpDispatcher : IEventDispatcher
{
    public Task Publish<TEvent>(TEvent e, CancellationToken ct) where TEvent : IIntegrationEvent
        => Task.CompletedTask;
}

public class UserServiceTests
{
    private static UserService NewService() => new(new InMemoryUserStore(), new NoOpDispatcher());

    [Fact]
    public async Task Create_then_List_returns_the_user()
    {
        var service = NewService();
        var created = await service.Create("Ada", "ada@example.com", TestContext.Current.CancellationToken);
        created.IsSuccess.ShouldBeTrue();

        var list = await service.List(TestContext.Current.CancellationToken);
        list.Value!.ShouldHaveSingleItem().Email.ShouldBe("ada@example.com");
    }

    [Fact]
    public async Task Create_with_duplicate_email_is_a_conflict()
    {
        var service = NewService();
        await service.Create("Ada", "ada@example.com", TestContext.Current.CancellationToken);
        var again = await service.Create("Grace", "ada@example.com", TestContext.Current.CancellationToken);
        again.IsSuccess.ShouldBeFalse();
        again.Error!.Kind.ShouldBe(ErrorKind.Conflict);
    }
}
