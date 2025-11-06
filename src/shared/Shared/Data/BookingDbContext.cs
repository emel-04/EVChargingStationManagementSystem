using EVChargingStation.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.Shared.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }

    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Booking configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BookingNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.BookingNumber).IsUnique();
            entity.Property(e => e.EnergyConsumed).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.QRCode).HasMaxLength(1000);
             
        });
    }
}



