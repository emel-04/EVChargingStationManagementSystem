using EVCharging.PaymentService.Data;
using EVCharging.PaymentService.DTOs;
using EVCharging.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace EVCharging.PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IPricingService _pricingService;

    public PaymentService(PaymentDbContext context, IPricingService pricingService)
    {
        _context = context;
        _pricingService = pricingService;
    }

    public async Task<PaymentDto> CreatePaymentAsync(long userId, CreatePaymentRequest request)
    {
        // Validate payment method
        var validMethods = new[] { "Wallet", "CreditCard", "BankTransfer", "EWallet" };
        if (!validMethods.Contains(request.PaymentMethod))
            throw new InvalidOperationException("Invalid payment method");

        // Check if wallet exists for user
        var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
        {
            wallet = new UserWallet
            {
                UserId = userId,
                Balance = 0,
                TotalTopUp = 0,
                TotalSpent = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserWallets.Add(wallet);
            await _context.SaveChangesAsync();
        }

        // Create payment
        var paymentCode = GeneratePaymentCode();
        var payment = new Payment
        {
            PaymentCode = paymentCode,
            BookingId = request.BookingId,
            UserId = userId,
            Amount = request.Amount,
            Currency = "VND",
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = "Pending",
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return MapToPaymentDto(payment);
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(long paymentId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId && !p.IsDeleted);

        return payment == null ? null : MapToPaymentDto(payment);
    }

    public async Task<PaymentDto?> GetPaymentByCodeAsync(string paymentCode)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentCode == paymentCode && !p.IsDeleted);

        return payment == null ? null : MapToPaymentDto(payment);
    }

    public async Task<List<PaymentDto>> GetUserPaymentsAsync(long userId, int pageNumber = 1, int pageSize = 10)
    {
        var payments = await _context.Payments
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return payments.Select(MapToPaymentDto).ToList();
    }

    public async Task<bool> CompletePaymentAsync(long paymentId, string transactionId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null || payment.IsDeleted || payment.PaymentStatus != "Pending")
            return false;

        payment.PaymentStatus = "Completed";
        payment.TransactionId = transactionId;
        payment.UpdatedAt = DateTime.UtcNow;
        _context.Payments.Update(payment);

        // Update wallet if payment method is Wallet
        if (payment.PaymentMethod == "Wallet")
        {
            var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == payment.UserId);
            if (wallet != null)
            {
                wallet.Balance -= payment.Amount;
                wallet.TotalSpent += payment.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _context.UserWallets.Update(wallet);

                // Record transaction
                var transaction = new WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    Amount = payment.Amount,
                    TransactionType = "Charge",
                    Description = $"Charging payment for booking",
                    RelatedPaymentId = paymentId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.WalletTransactions.Add(transaction);
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> FailPaymentAsync(long paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null || payment.IsDeleted)
            return false;

        payment.PaymentStatus = "Failed";
        payment.UpdatedAt = DateTime.UtcNow;
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RefundPaymentAsync(long paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null || payment.IsDeleted || payment.PaymentStatus != "Completed")
            return false;

        payment.PaymentStatus = "Refunded";
        payment.UpdatedAt = DateTime.UtcNow;
        _context.Payments.Update(payment);

        // Refund to wallet
        if (payment.PaymentMethod == "Wallet")
        {
            var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == payment.UserId);
            if (wallet != null)
            {
                wallet.Balance += payment.Amount;
                wallet.TotalSpent -= payment.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _context.UserWallets.Update(wallet);

                var transaction = new WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    Amount = payment.Amount,
                    TransactionType = "Refund",
                    Description = $"Refund for payment {payment.PaymentCode}",
                    RelatedPaymentId = paymentId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.WalletTransactions.Add(transaction);
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private string GeneratePaymentCode()
    {
        return $"PM{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }

    private PaymentDto MapToPaymentDto(Payment payment)
    {
        return new PaymentDto
        {
            PaymentId = payment.PaymentId,
            PaymentCode = payment.PaymentCode,
            BookingId = payment.BookingId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            TransactionId = payment.TransactionId,
            CreatedAt = payment.CreatedAt
        };
    }
}