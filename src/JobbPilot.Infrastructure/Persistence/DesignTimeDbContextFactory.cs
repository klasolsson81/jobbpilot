using Microsoft.EntityFrameworkCore.Design;

namespace JobbPilot.Infrastructure.Persistence;

/// <summary>
/// Används av `dotnet ef migrations add` utan faktisk app-host. Delegerar till
/// <see cref="MigrationsOptionsFactory.BuildAppOptions"/> — single source of truth för
/// EF-konfig per ADR 0034.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=jobbpilot;Username=jobbpilot;Password=jobbpilot";
        return new AppDbContext(MigrationsOptionsFactory.BuildAppOptions(cs));
    }
}
