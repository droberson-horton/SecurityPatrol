using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecurityPatrol.Web.Models;

namespace SecurityPatrol.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Building> Buildings { get; set; }
    public DbSet<Floor> Floors { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<PatrolRoute> PatrolRoutes { get; set; }
    public DbSet<PatrolRouteLocation> PatrolRouteLocations { get; set; }
    public DbSet<ScheduledPatrol> ScheduledPatrols { get; set; }
    public DbSet<Patrol> Patrols { get; set; }
    public DbSet<PatrolScan> PatrolScans { get; set; }
    public DbSet<PatrolScanNote> PatrolScanNotes { get; set; }
    public DbSet<AdminMessage> AdminMessages { get; set; }
    public DbSet<ScheduleTemplate> ScheduleTemplates { get; set; }
    public DbSet<ScheduleTemplateSlot> ScheduleTemplateSlots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Building
        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
        });

        // Floor
        modelBuilder.Entity<Floor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Building)
                .WithMany(b => b.Floors)
                .HasForeignKey(e => e.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Location
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.QrCode).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.QrCode).IsUnique();
            entity.HasOne(e => e.Building)
                .WithMany(b => b.Locations)
                .HasForeignKey(e => e.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Floor)
                .WithMany(f => f.Locations)
                .HasForeignKey(e => e.FloorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PatrolRoute
        modelBuilder.Entity<PatrolRoute>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // PatrolRouteLocation
        modelBuilder.Entity<PatrolRouteLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.PatrolRoute)
                .WithMany(r => r.PatrolRouteLocations)
                .HasForeignKey(e => e.PatrolRouteId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ScheduledPatrol
        modelBuilder.Entity<ScheduledPatrol>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Officer)
                .WithMany()
                .HasForeignKey(e => e.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PatrolRoute)
                .WithMany()
                .HasForeignKey(e => e.PatrolRouteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Patrol
        modelBuilder.Entity<Patrol>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Officer)
                .WithMany()
                .HasForeignKey(e => e.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ScheduledPatrol)
                .WithMany(s => s.Patrols)
                .HasForeignKey(e => e.ScheduledPatrolId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PatrolScan
        modelBuilder.Entity<PatrolScan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Patrol)
                .WithMany(w => w.PatrolScans)
                .HasForeignKey(e => e.PatrolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PatrolScanNote
        modelBuilder.Entity<PatrolScanNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.PatrolScan)
                .WithMany(s => s.PatrolScanNotes)
                .HasForeignKey(e => e.PatrolScanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AdminMessage
        modelBuilder.Entity<AdminMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Body).IsRequired().HasMaxLength(4000);
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScheduleTemplate>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });
        modelBuilder.Entity<ScheduleTemplateSlot>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Template).WithMany(t => t.Slots).HasForeignKey(e => e.TemplateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PatrolRoute).WithMany().HasForeignKey(e => e.PatrolRouteId).OnDelete(DeleteBehavior.SetNull);
        });

        // Note: admin user is seeded in Program.cs via UserManager (not here)
        // because Identity password hashing must go through UserManager to work correctly.

        // Seed Building
        modelBuilder.Entity<Building>().HasData(new Building
        {
            Id = 1,
            Name = "Main Building",
            Address = "123 Security Way, Suite 100",
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed Floor
        modelBuilder.Entity<Floor>().HasData(new Floor
        {
            Id = 1,
            BuildingId = 1,
            Name = "Floor 1",
            FloorNumber = 1,
            IsActive = true
        });

        // Seed Locations
        modelBuilder.Entity<Location>().HasData(
            new Location
            {
                Id = 1,
                BuildingId = 1,
                FloorId = 1,
                Name = "Main Entrance",
                Description = "Front lobby entrance",
                QrCode = "loc-main-entrance-001",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Location
            {
                Id = 2,
                BuildingId = 1,
                FloorId = 1,
                Name = "Server Room",
                Description = "Data center on floor 1",
                QrCode = "loc-server-room-002",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Location
            {
                Id = 3,
                BuildingId = 1,
                FloorId = 1,
                Name = "Emergency Exit A",
                Description = "Emergency exit on east side",
                QrCode = "loc-emergency-exit-a-003",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed PatrolRoute
        modelBuilder.Entity<PatrolRoute>().HasData(new PatrolRoute
        {
            Id = 1,
            Name = "Standard Floor 1 Route",
            Description = "Default patrol route covering all floor 1 checkpoints",
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed PatrolRouteLocations
        modelBuilder.Entity<PatrolRouteLocation>().HasData(
            new PatrolRouteLocation { Id = 1, PatrolRouteId = 1, LocationId = 1, OrderIndex = 1 },
            new PatrolRouteLocation { Id = 2, PatrolRouteId = 1, LocationId = 2, OrderIndex = 2 },
            new PatrolRouteLocation { Id = 3, PatrolRouteId = 1, LocationId = 3, OrderIndex = 3 }
        );
    }
}
