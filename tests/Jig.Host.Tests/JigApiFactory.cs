using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Jig.Host.Tests;

// Boots the real host with a private SQLite file per factory, so booting for a test neither
// clobbers the dev jig.db nor collides with another test. Everything else is the real wiring.
internal sealed class JigApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"jig-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "EfCore",
                ["Persistence:ConnectionString"] = $"Data Source={_dbPath}",
                // Force telemetry export off regardless of any ambient OTEL endpoint on the box,
                // so booting the host in a test never attaches a real OTLP exporter.
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "",
            }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Best effort: SQLite's connection pool can still hold the handle at this point. The file
        // is a uniquely-named, gitignored temp file, so leaving it for the OS to reap is harmless.
        try
        {
            if (disposing && File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }
}
