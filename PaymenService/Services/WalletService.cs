using EVCharging.PaymentService.Data;
using EVCharging.PaymentService.DTOs;
using EVCharging.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace EVCharging.PaymentService.Services;

public class WalletService : IWalletService
{
    private readonly PaymentDbContext _context;

    public WalletService(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<WalletDto?> GetWalletAsync(long userId)
    {
        var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        return wallet == null ? null : MapToWalletDto(wallet);
    }

    public async Task<WalletDto> TopUpWalletAsync(long userId, TopUpWalletRequest request)
    {
        var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
        {
            wallet = new UserWallet
            {
                UserId = userId,
                Balance = request.Amount,
                TotalTopUp = request.Amount,
                TotalSpent = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserWallets.Add(wallet);
        }
        else
        {
            wallet.Balance += request.Amount;
            wallet.TotalTopUp += request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            _context.UserWallets.Update(wallet);
        }

        // Record transaction
        var transaction = new WalletTransaction
        {
            WalletId = wallet.WalletId,
            Amount = request.Amount,
            TransactionType = "TopUp",
            Description = $"Top up via {request.PaymentMethod}",
            CreatedAt = DateTime.UtcNow
        };
        _context.WalletTransactions.Add(transaction);

        await _context.SaveChangesAsync();
        return MapToWalletDto(wallet);
    }

    public async Task<bool> DeductFromWalletAsync(long userId, decimal amount)
    {
        var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null || wallet.Balance < amount)
            return false;

        wallet.Balance -= amount;
        wallet.TotalSpent += amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        _context.UserWallets.Update(wallet);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RefundToWalletAsync(long userId, decimal amount)
    {
        var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return false;

        wallet.Balance += amount;
        wallet.TotalSpent -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        _context.UserWallets.Update(wallet);
        await _context.SaveChangesAsync();

        return true;
    }

    private WalletDto MapToWalletDto(UserWallet wallet)
    {
        return new WalletDto
        {
            WalletId = wallet.WalletId,
            UserId = wallet.UserId,
            Balance = wallet.Balance,
            TotalTopUp = wallet.TotalTopUp,
            TotalSpent = wallet.TotalSpent
        };
    }
}