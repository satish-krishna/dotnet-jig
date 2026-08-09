using Jig.Modules.Users.Application;
using Jig.Modules.Users.Infrastructure;
using Jig.Modules.Users.Contracts;
using Jig.SharedKernel;
using Shouldly;
using Xunit;

namespace Jig.Modules.Users.Tests;

public class UserServiceEventTests
{
    private sealed class CapturingDispatcher : IEventDispatcher
    {
        public List<IIntegrationEvent> Published { get; } = [];
        public Task Publish<TEvent>(TEvent e, CancellationToken ct) where TEvent : IIntegrationEvent
        { Published.Add(e); return Task.CompletedTask; }
    }

    [Fact]
    public async Task Create_publishes_UserRegistered()
    {
        var events = new CapturingDispatcher();
        var service = new UserService(new InMemoryUserStore(), events);

        await service.Create("Ada", "ada@x.com", TestContext.Current.CancellationToken);

        var evt = events.Published.ShouldHaveSingleItem().ShouldBeOfType<UserRegistered>();
        evt.Email.ShouldBe("ada@x.com");
    }
}
