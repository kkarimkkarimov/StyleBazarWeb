using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Stylebazar.DAL.Contexts;

// DESIGN-TIME (dizayn vaxtı) factory — YALNIZ EF alətləri üçün.
// "dotnet ef migrations add ..." və ya "database update" əmrlərini işlədəndə EF
// işləyən tətbiqi başlatmadan DbContext-i necə yaradacağını bilməlidir. Bu sinif ona
// hazır connection string ilə kontekst qaytarır. Tətbiqin işləmə vaxtına (runtime) DƏXLİ YOXDUR.
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    // Yalnız migration əmrləri üçün lokal baza ünvanı (production-da işlənmir).
    private const string DesignTimeConnectionString =
        "Server=localhost;Database=StyleBazarDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(DesignTimeConnectionString);  // SQL Server provayderi
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
