namespace EVCharging.PaymentService.Models;

public class WalletTransaction
{
    public long TransactionId { get; set; }
    public long WalletId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty; // TopUp, Charge, Refund
    public string? Description { get; set; }
    public long? RelatedPaymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
