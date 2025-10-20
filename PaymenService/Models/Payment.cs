namespace EVCharging.PaymentService.Models;

public class Payment
{
    public long PaymentId { get; set; }
    public string PaymentCode { get; set; } = string.Empty;
    public long BookingId { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string PaymentMethod { get; set; } = string.Empty; // Wallet, CreditCard, BankTransfer, EWallet
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded
    public string? TransactionId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}