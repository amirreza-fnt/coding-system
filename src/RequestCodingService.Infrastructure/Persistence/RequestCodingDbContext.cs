using Microsoft.EntityFrameworkCore;
using RequestCodingService.Domain.Entities;
using RequestCodingService.Domain.Enums;

namespace RequestCodingService.Infrastructure.Persistence;

public sealed class RequestCodingDbContext : DbContext
{
    public RequestCodingDbContext(DbContextOptions<RequestCodingDbContext> options)
        : base(options)
    {
    }

    public DbSet<TrackingRequest> TrackingRequests => Set<TrackingRequest>();
    public DbSet<RequestCounter> RequestCounters => Set<RequestCounter>();
    public DbSet<SystemDefinition> Systems => Set<SystemDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SystemDefinition>(e =>
        {
            e.ToTable("Systems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<RequestCounter>(e =>
        {
            e.ToTable("RequestCounters");
            e.HasKey(x => x.Id);
            e.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.SystemId, x.NationalCode }).IsUnique();
            e.HasOne(x => x.System).WithMany().HasForeignKey(x => x.SystemId);
        });

        modelBuilder.Entity<TrackingRequest>(e =>
        {
            e.ToTable("TrackingRequests");
            e.HasKey(x => x.Id);

            e.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Mobile).HasMaxLength(15);
            e.Property(x => x.Landline).HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

            e.HasIndex(x => new { x.SystemId, x.NationalCode, x.Counter }).IsUnique();
            e.HasIndex(x => x.NationalCode);
            e.HasIndex(x => x.CreatedAtUtc);
            e.HasIndex(x => x.Mobile);
            e.HasIndex(x => x.Landline);

            e.HasOne(x => x.System).WithMany().HasForeignKey(x => x.SystemId);

            e.HasQueryFilter(x => x.DeletedAtUtc == null);
        });

        SeedSystems(modelBuilder);
    }

    private static void SeedSystems(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<SystemDefinition>().HasData(
            new SystemDefinition
            {
                Id = 1,
                Code = "137",
                Name = "سامانه ۱۳۷",
                IsActive = true,
                CreatedAtUtc = seedDate
            },
            new SystemDefinition
            {
                Id = 2,
                Code = "FIRE",
                Name = "آتش‌نشانی",
                IsActive = true,
                CreatedAtUtc = seedDate
            });
    }
}
