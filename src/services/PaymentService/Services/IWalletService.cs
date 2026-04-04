using EVChargingStation.Shared.Models;

namespace EVChargingStation.PaymentService.Services;

public interface IWalletService
{
    Task<Wallet?> GetWalletByUserIdAsync(int userId);
    Task<Wallet> CreateWalletAsync(int userId);
    Task<decimal> GetWalletBalanceAsync(int userId);
    Task<bool> AddToWalletAsync(int userId, decimal amount, string? description = null);
    Task<bool> DeductFromWalletAsync(int userId, decimal amount, string? description = null);
    Task<IEnumerable<WalletTransaction>> GetWalletTransactionsAsync(int userId);
    Task<IEnumerable<WalletTransaction>> GetWalletTransactionsAsync(int userId, DateTime fromDate, DateTime toDate);
    Task<bool> TransferBetweenWalletsAsync(int fromUserId, int toUserId, decimal amount, string? description = null);
}

public class WalletTransactionRequest
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string? Description { get; set; }
}






