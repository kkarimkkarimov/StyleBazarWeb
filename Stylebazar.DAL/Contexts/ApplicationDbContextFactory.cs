using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Stylebazar.DAL.Contexts;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=localhost;Database=StyleBazarDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(DesignTimeConnectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
