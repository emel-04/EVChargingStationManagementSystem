namespace EVCharging.PaymentService.Models;

public class UserWallet
{
    public long WalletId { get; set; }
    public long UserId { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalTopUp { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}