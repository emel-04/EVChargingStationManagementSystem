using EVChargingStation.Shared.Models;

namespace EVChargingStation.PaymentService.Services;

public interface IPaymentService
{
    Task<Payment?> GetPaymentByIdAsync(int id);
    Task<Payment?> GetPaymentByNumberAsync(string paymentNumber);
    Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(int userId);
    Task<IEnumerable<Payment>> GetPaymentsByBookingIdAsync(int bookingId);
    Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);
    Task<Payment> CreatePaymentAsync(CreatePaymentRequest request);
    Task<Payment> ProcessPaymentAsync(int id, ProcessPaymentRequest request);
    Task<bool> RefundPaymentAsync(int id, RefundPaymentRequest request);
    Task<IEnumerable<Payment>> GetAllPaymentsAsync();
    Task<Payment> CreatePaymentForBookingAsync(int bookingId, decimal amount, PaymentMethod method);
}

public class CreatePaymentRequest
{
    public int UserId { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Description { get; set; }
}

public class ProcessPaymentRequest
{
    public string? TransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Description { get; set; }
}

public class RefundPaymentRequest
{
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}






