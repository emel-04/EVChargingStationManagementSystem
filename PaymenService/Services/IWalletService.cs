using EVCharging.PaymentService.DTOs;

namespace EVCharging.PaymentService.Services;

public interface IWalletService
{
    Task<WalletDto?> GetWalletAsync(long userId);
    Task<WalletDto> TopUpWalletAsync(long userId, TopUpWalletRequest request);
    Task<bool> DeductFromWalletAsync(long userId, decimal amount);
    Task<bool> RefundToWalletAsync(long userId, decimal amount);
}