namespace EVCharging.PaymentService.DTOs;

public class TopUpWalletRequest
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // CreditCard, BankTransfer, EWallet
}