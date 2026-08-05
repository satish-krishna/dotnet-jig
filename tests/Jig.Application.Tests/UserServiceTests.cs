using Jig.Application;
using Shouldly;

namespace Jig.Application.Tests;

public class UserServiceTests
{
    [Fact]
    public void Create_assigns_an_id_and_Find_returns_the_user()
    {
        var users = new UserService();

        var created = users.Create("Ada Lovelace", "ada@example.com");

        created.Id.ShouldBeGreaterThan(0);
        users.Find(created.Id).ShouldBe(created);
    }

    [Fact]
    public void Find_returns_null_for_an_unknown_id()
    {
        var users = new UserService();

        users.Find(404).ShouldBeNull();
    }

    [Fact]
    public void All_returns_every_created_user()
    {
        var users = new UserService();
        users.Create("Ada", "ada@example.com");
        users.Create("Alan", "alan@example.com");

        users.All().Count.ShouldBe(2);
    }
}
