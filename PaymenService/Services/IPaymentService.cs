using EVCharging.PaymentService.DTOs;

namespace EVCharging.PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentDto> CreatePaymentAsync(long userId, CreatePaymentRequest request);
    Task<PaymentDto?> GetPaymentByIdAsync(long paymentId);
    Task<PaymentDto?> GetPaymentByCodeAsync(string paymentCode);
    Task<List<PaymentDto>> GetUserPaymentsAsync(long userId, int pageNumber = 1, int pageSize = 10);
    Task<bool> CompletePaymentAsync(long paymentId, string transactionId);
    Task<bool> FailPaymentAsync(long paymentId);
    Task<bool> RefundPaymentAsync(long paymentId);
}