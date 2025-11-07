using System.ComponentModel.DataAnnotations;

namespace EVChargingStation.Shared.Models;

public class Payment
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public int BookingId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string PaymentNumber { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    
    public PaymentMethod Method { get; set; }
    
    public PaymentStatus Status { get; set; }
    
    [StringLength(500)]
    public string? TransactionId { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Booking Booking { get; set; } = null!;
}

public enum PaymentMethod
{
    CreditCard = 1,
    DebitCard = 2,
    EWallet = 3,
    BankTransfer = 4,
    Cash = 5,
    Voucher = 6
}

public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Refunded = 5,
    Cancelled = 6
}

public class Wallet
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    public decimal Balance { get; set; } = 0;
    
    public decimal TotalDeposited { get; set; } = 0;
    
    public decimal TotalSpent { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}

public class WalletTransaction
{
    public int Id { get; set; }
    
    [Required]
    public int WalletId { get; set; }
    
    public decimal Amount { get; set; }
    
    public TransactionType Type { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Wallet Wallet { get; set; } = null!;
}

public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Payment = 3,
    Refund = 4,
    Bonus = 5
}


