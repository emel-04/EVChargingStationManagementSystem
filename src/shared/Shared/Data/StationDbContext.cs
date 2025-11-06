using EVChargingStation.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.Shared.Data;

public class StationDbContext : DbContext
{
    public StationDbContext(DbContextOptions<StationDbContext> options) : base(options)
    {
    }

    public DbSet<ChargingStation> ChargingStations { get; set; }
    public DbSet<ChargingPoint> ChargingPoints { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ChargingStation configuration
        modelBuilder.Entity<ChargingStation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.Province).HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<int>();
        });

        // ChargingPoint configuration
        modelBuilder.Entity<ChargingPoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PointNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ConnectorType).HasConversion<int>();
            entity.Property(e => e.PricePerKwh).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PricePerHour).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasOne(e => e.Station)
                  .WithMany(e => e.ChargingPoints)
                  .HasForeignKey(e => e.StationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}



