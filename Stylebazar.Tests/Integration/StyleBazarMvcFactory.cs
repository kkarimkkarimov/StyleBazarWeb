using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Stylebazar.Tests.Integration;

/// <summary>
/// Boots the real MVC app in-memory (TestServer) for integration tests, pointed
/// at a dedicated test database so a run never touches dev data. The app's own
/// startup MigrateAsync creates + migrates that DB on first boot. Runs in the
/// Development environment so the dev secret fallbacks (Jwt:Key, Admin:Password)
/// fill in without real env vars.
/// </summary>
public sealed class StyleBazarMvcFactory : WebApplicationFactory<Program>
{
    /// <summary>Test DB. Override locally / in CI via the TEST_DB_CONNECTION env var.</summary>
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Server=localhost;Database=StyleBazarDb_Test;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False";

    /// <summary>
    /// True when the SQL Server is reachable. Lets integration tests fail fast
    /// with an actionable message instead of an opaque app-boot exception.
    /// </summary>
    public bool DatabaseReachable { get; } = ProbeDatabase(ConnectionString);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString
            });
        });
    }

    private static bool ProbeDatabase(string connectionString)
    {
        try
        {
            var csb = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master",
                ConnectTimeout = 3
            };
            using var conn = new SqlConnection(csb.ConnectionString);
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
