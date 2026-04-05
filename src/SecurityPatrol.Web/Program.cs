using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Data;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Persist Data Protection keys so antiforgery tokens survive restarts/multi-replica deployments
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/dataprotection-keys"))
    .SetApplicationName("SecurityPatrol");

// Add services
builder.Services.AddControllersWithViews();

// EF Core + SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie authentication settings
// Role claims are stored in the Identity claims store at login (AccountController.Login)
// and loaded into the cookie automatically — no OnValidatePrincipal needed.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(ClaimTypes.Role, UserRole.Administrator.ToString())));

    options.AddPolicy("OfficerAndAdmin", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(ClaimTypes.Role, UserRole.Administrator.ToString()) ||
            ctx.User.HasClaim(ClaimTypes.Role, UserRole.SecurityOfficer.ToString())));

    options.AddPolicy("AuditorAndAdmin", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(ClaimTypes.Role, UserRole.Administrator.ToString()) ||
            ctx.User.HasClaim(ClaimTypes.Role, UserRole.Auditor.ToString())));
});

// Register application services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPatrolService, PatrolService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

// Auto-run migrations and seed data on startup — retry until SQL Server is ready
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 20;
    const int delaySeconds = 10;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("Database initialization attempt {Attempt}/{Max}...", attempt, maxRetries);
            db.Database.Migrate();
            logger.LogInformation("Database schema ready.");

            // Seed admin user via UserManager so password hashing works correctly
            var existingAdmin = await userManager.FindByEmailAsync("admin@drhsecurity.com");
            if (existingAdmin == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@drhsecurity.com",
                    Email = "admin@drhsecurity.com",
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    Role = UserRole.Administrator,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    logger.LogInformation("Admin user created: admin@drhsecurity.com / Admin@123");
                else
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            else
            {
                // Ensure admin is never permanently locked out (e.g. from failed attempts during setup)
                if (await userManager.IsLockedOutAsync(existingAdmin))
                {
                    await userManager.SetLockoutEndDateAsync(existingAdmin, DateTimeOffset.UtcNow.AddSeconds(-1));
                    await userManager.ResetAccessFailedCountAsync(existingAdmin);
                    logger.LogInformation("Admin lockout cleared.");
                }
            }

            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxRetries, ex.Message);
            if (attempt == maxRetries)
            {
                logger.LogError(ex, "Could not connect to the database after {Max} attempts.", maxRetries);
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }
}

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = exceptionFeature?.Error;
        var detail = ex != null ? System.Net.WebUtility.HtmlEncode($"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}") : "No exception details available.";

        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync($"""
            <!DOCTYPE html><html><head><title>Error</title>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css"/>
            </head><body class="bg-light"><div class="container mt-5">
            <h2 class="text-danger">An error occurred</h2>
            <pre class="bg-white border rounded p-3 small text-danger" style="overflow:auto;max-height:60vh">{detail}</pre>
            <a href="/" class="btn btn-primary mt-3">Return to Home</a>
            </div></body></html>
            """);
    });
});

// HTTPS redirection only when explicitly running HTTPS (not in Docker HTTP-only mode)
if (!string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_HTTPS_PORTS"]))
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint for Kubernetes probes
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

// Root path goes to login page
app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Account", action = "Login" });

// All other routes default action to Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.Run();
