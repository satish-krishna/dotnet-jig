namespace Jig.Modules.Users.Infrastructure;

/// <summary>Which store backs the module and how to reach it. <see cref="Provider"/> is read at
/// registration time to choose between the EF Core and Mongo stores; the connection details are
/// resolved from whichever provider wins.</summary>
internal sealed class PersistenceOptions
{
    public string Provider { get; set; } = "EfCore";

    public string ConnectionString { get; set; } = "";

    public string DatabaseName { get; set; } = "jig";
}
