using EVChargingStation.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.Shared.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.PaymentNumber).IsUnique();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Method).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.TransactionId).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
     
            entity.Ignore(e => e.User);
            entity.Ignore(e => e.Booking);
            
        
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.BookingId).IsRequired();
        });


        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Balance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDeposited).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalSpent).HasColumnType("decimal(18,2)");
            

            entity.Ignore(e => e.User);
            
            entity.Property(e => e.UserId).IsRequired();
        });


        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasOne(e => e.Wallet)
                  .WithMany(e => e.Transactions)
                  .HasForeignKey(e => e.WalletId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}