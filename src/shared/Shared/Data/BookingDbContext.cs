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
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.EnergyConsumed).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.QRCode).HasMaxLength(1000);
            entity.Property(e => e.StationId).IsRequired();
            
            // Booking Service sở hữu riêng bảng Bookings nên cần bỏ qua
            // các navigation sang entity thuộc service khác để tránh EF tạo bảng/khóa ngoại phụ.
            entity.Ignore(e => e.User);
            entity.Ignore(e => e.ChargingPoint);
            entity.Ignore(e => e.Payments);
        });
    }
}



