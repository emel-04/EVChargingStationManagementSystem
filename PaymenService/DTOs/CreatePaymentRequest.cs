namespace EVCharging.PaymentService.DTOs;

public class CreatePaymentRequest
{
    public long BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // Wallet, CreditCard, BankTransfer, EWallet
    public string? PromotionCode { get; set; }
    public string? Description { get; set; }
}