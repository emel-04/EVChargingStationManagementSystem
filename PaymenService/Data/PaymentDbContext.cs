using Microsoft.EntityFrameworkCore;
using EVCharging.PaymentService.Models;

namespace EVCharging.PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }
    public DbSet<UserWallet> UserWallets { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.PaymentCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.PaymentCode).IsUnique();
            entity.HasIndex(e => e.BookingId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PaymentStatus);
        });

        modelBuilder.Entity<UserWallet>(entity =>
        {
            entity.HasKey(e => e.WalletId);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.HasIndex(e => e.WalletId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}