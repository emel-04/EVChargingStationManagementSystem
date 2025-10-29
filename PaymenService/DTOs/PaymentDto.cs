namespace EVCharging.PaymentService.DTOs;

public class PaymentDto
{
    public long PaymentId { get; set; }
    public string PaymentCode { get; set; } = string.Empty;
    public long BookingId { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}