namespace EVCharging.PaymentService.DTOs;

public class WalletDto
{
    public long WalletId { get; set; }
    public long UserId { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalTopUp { get; set; }
    public decimal TotalSpent { get; set; }
}