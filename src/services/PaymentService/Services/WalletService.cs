using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.PaymentService.Services;

public class WalletService : IWalletService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<WalletService> _logger;

    public WalletService(PaymentDbContext context, ILogger<WalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Wallet?> GetWalletByUserIdAsync(int userId)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId && w.IsActive);
    }

    public async Task<Wallet> CreateWalletAsync(int userId)
    {
        // Check if wallet already exists
        var existingWallet = await GetWalletByUserIdAsync(userId);
        if (existingWallet != null)
        {
            throw new InvalidOperationException("Wallet already exists for this user");
        }

        var wallet = new Wallet
        {
            UserId = userId,
            Balance = 0,
            TotalDeposited = 0,
            TotalSpent = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Wallet created for user: {UserId}", userId);

        return wallet;
    }

    public async Task<decimal> GetWalletBalanceAsync(int userId)
    {
        var wallet = await GetWalletByUserIdAsync(userId);
        return wallet?.Balance ?? 0;
    }

    public async Task<bool> AddToWalletAsync(int userId, decimal amount, string? description = null)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive");
        }

        var wallet = await GetWalletByUserIdAsync(userId);
        if (wallet == null)
        {
            wallet = await CreateWalletAsync(userId);
        }

        wallet.Balance += amount;
        wallet.TotalDeposited += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        // Create transaction record
        var transaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = TransactionType.Deposit,
            Description = description ?? "Wallet deposit",
            CreatedAt = DateTime.UtcNow
        };

        _context.WalletTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added {Amount} to wallet for user {UserId}", amount, userId);

        return true;
    }

    public async Task<bool> DeductFromWalletAsync(int userId, decimal amount, string? description = null)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive");
        }

        var wallet = await GetWalletByUserIdAsync(userId);
        if (wallet == null)
        {
            throw new InvalidOperationException("Wallet not found");
        }

        if (wallet.Balance < amount)
        {
            throw new InvalidOperationException("Insufficient wallet balance");
        }

        wallet.Balance -= amount;
        wallet.TotalSpent += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        // Create transaction record
        var transaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = -amount, // Negative amount for deduction
            Type = TransactionType.Payment,
            Description = description ?? "Wallet payment",
            CreatedAt = DateTime.UtcNow
        };

        _context.WalletTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deducted {Amount} from wallet for user {UserId}", amount, userId);

        return true;
    }

    public async Task<IEnumerable<WalletTransaction>> GetWalletTransactionsAsync(int userId)
    {
        var wallet = await GetWalletByUserIdAsync(userId);
        if (wallet == null)
        {
            return new List<WalletTransaction>();
        }

        return await _context.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<WalletTransaction>> GetWalletTransactionsAsync(int userId, DateTime fromDate, DateTime toDate)
    {
        var wallet = await GetWalletByUserIdAsync(userId);
        if (wallet == null)
        {
            return new List<WalletTransaction>();
        }

        return await _context.WalletTransactions
            .Where(t => t.WalletId == wallet.Id && 
                       t.CreatedAt >= fromDate && 
                       t.CreatedAt <= toDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> TransferBetweenWalletsAsync(int fromUserId, int toUserId, decimal amount, string? description = null)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive");
        }

        if (fromUserId == toUserId)
        {
            throw new ArgumentException("Cannot transfer to the same wallet");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Deduct from source wallet
            await DeductFromWalletAsync(fromUserId, amount, 
                description ?? $"Transfer to user {toUserId}");

            // Add to destination wallet
            await AddToWalletAsync(toUserId, amount, 
                description ?? $"Transfer from user {fromUserId}");

            await transaction.CommitAsync();

            _logger.LogInformation("Transferred {Amount} from user {FromUserId} to user {ToUserId}", 
                amount, fromUserId, toUserId);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}


