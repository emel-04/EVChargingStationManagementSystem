using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IWalletService _walletService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(PaymentDbContext context, IWalletService walletService, ILogger<PaymentService> logger)
    {
        _context = context;
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<Payment?> GetPaymentByIdAsync(int id)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetPaymentByNumberAsync(string paymentNumber)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentNumber == paymentNumber);
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByUserIdAsync(int userId)
    {
        return await _context.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByBookingIdAsync(int bookingId)
    {
        return await _context.Payments
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status)
    {
        return await _context.Payments
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Payment> CreatePaymentAsync(CreatePaymentRequest request)
    {
        var paymentNumber = GeneratePaymentNumber();

        var payment = new Payment
        {
            UserId = request.UserId,
            BookingId = request.BookingId,
            PaymentNumber = paymentNumber,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Pending,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment created successfully: {PaymentNumber}", payment.PaymentNumber);

        return payment;
    }

    public async Task<Payment> ProcessPaymentAsync(int id, ProcessPaymentRequest request)
    {
        var payment = await GetPaymentByIdAsync(id);
        if (payment == null)
        {
            throw new ArgumentException("Payment not found");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Payment is not in pending status");
        }

        payment.Status = request.Status;
        payment.TransactionId = request.TransactionId;
        payment.Description = request.Description;
        payment.ProcessedAt = DateTime.UtcNow;

        // If payment is successful, update wallet if using e-wallet
        if (request.Status == PaymentStatus.Completed && payment.Method == PaymentMethod.EWallet)
        {
            await _walletService.DeductFromWalletAsync(payment.UserId, payment.Amount, 
                $"Payment for booking {payment.BookingId}");
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment processed: {PaymentNumber} - {Status}", payment.PaymentNumber, payment.Status);

        return payment;
    }

    public async Task<bool> RefundPaymentAsync(int id, RefundPaymentRequest request)
    {
        var payment = await GetPaymentByIdAsync(id);
        if (payment == null)
        {
            return false;
        }

        if (payment.Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException("Only completed payments can be refunded");
        }

        // Create refund payment
        var refundPayment = new Payment
        {
            UserId = payment.UserId,
            BookingId = payment.BookingId,
            PaymentNumber = GeneratePaymentNumber(),
            Amount = -request.Amount, // Negative amount for refund
            Method = payment.Method,
            Status = PaymentStatus.Completed,
            Description = $"Refund: {request.Reason}",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };

        _context.Payments.Add(refundPayment);

        // Update original payment status
        payment.Status = PaymentStatus.Refunded;
        // payment.UpdatedAt = DateTime.UtcNow; // Removed - UpdatedAt property doesn't exist

        // If using e-wallet, add money back to wallet
        if (payment.Method == PaymentMethod.EWallet)
        {
            await _walletService.AddToWalletAsync(payment.UserId, request.Amount, 
                $"Refund for payment {payment.PaymentNumber}");
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment refunded: {PaymentNumber} - Amount: {Amount}", 
            payment.PaymentNumber, request.Amount);

        return true;
    }

    public async Task<Payment> CreatePaymentForBookingAsync(int bookingId, decimal amount, PaymentMethod method)
    {
        // This would typically get the user ID from the booking
        // For now, we'll assume it's passed in the request
        var request = new CreatePaymentRequest
        {
            UserId = 1, // This should come from the booking
            BookingId = bookingId,
            Amount = amount,
            Method = method,
            Description = $"Payment for booking {bookingId}"
        };

        return await CreatePaymentAsync(request);
    }

    private string GeneratePaymentNumber()
    {
        return $"PAY{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }
}


