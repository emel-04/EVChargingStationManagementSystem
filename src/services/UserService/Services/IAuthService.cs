using EVChargingStation.Shared.Models;
using EVChargingStation.UserService.Models;

namespace EVChargingStation.UserService.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> VerifyEmailAsync(string email, string token);
}


