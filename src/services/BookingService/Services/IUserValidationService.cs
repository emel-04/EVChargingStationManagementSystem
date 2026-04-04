namespace EVChargingStation.BookingService.Services;

/// <summary>
/// Service để validate UserId có tồn tại trong User Service không
/// </summary>
public interface IUserValidationService
{
    /// <summary>
    /// Kiểm tra UserId có tồn tại trong User Service không
    /// </summary>
    /// <param name="userId">UserId cần kiểm tra</param>
    /// <returns>True nếu user tồn tại, False nếu không</returns>
    Task<bool> ValidateUserIdAsync(int userId);
}

