using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecurityPatrol.Web.Data;

/// <summary>
/// Used by `dotnet ef migrations` at design time so it doesn't need a live database
/// or the real connection string from appsettings / environment variables.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=SecurityPatrol_DesignTime;User Id=sa;Password=DesignTime@123;TrustServerCertificate=True;",
                sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .Options;

        return new ApplicationDbContext(options);
    }
}
