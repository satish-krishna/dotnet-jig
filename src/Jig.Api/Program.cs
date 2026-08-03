using System.Collections.Concurrent;

// The baseline: one project, one file, everything in front of you. This is the
// out-of-the-box shape a users API starts in. Every decision in the series is
// diffed against this. The store is in memory on purpose; persistence is its
// own decision later in the series.

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var users = new ConcurrentDictionary<int, User>();
var nextId = 0;

app.MapGet("/users", () => users.Values);

app.MapGet("/users/{id:int}", (int id) =>
    users.TryGetValue(id, out var user) ? Results.Ok(user) : Results.NotFound());

app.MapPost("/users", (CreateUser input) =>
{
    var id = Interlocked.Increment(ref nextId);
    var user = new User(id, input.Name, input.Email);
    users[id] = user;
    return Results.Created($"/users/{id}", user);
});

app.Run();

public record User(int Id, string Name, string Email);
public record CreateUser(string Name, string Email);
