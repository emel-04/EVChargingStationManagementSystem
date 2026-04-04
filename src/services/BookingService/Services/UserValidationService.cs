using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EVChargingStation.BookingService.Services;

/// <summary>
/// Service để validate UserId bằng cách gọi User Service API
/// </summary>
public class UserValidationService : IUserValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserValidationService> _logger;
    private readonly string _userServiceUrl;

    public UserValidationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<UserValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Lấy URL của User Service từ configuration hoặc API Gateway
        // Trong Docker, sử dụng service name; trong local, có thể dùng localhost
        _userServiceUrl = configuration["UserService:BaseUrl"] 
            ?? "http://user-service:80"; // Default cho Docker
    }

    public async Task<bool> ValidateUserIdAsync(int userId)
    {
        try
        {
            // Gọi User Service API để kiểm tra user có tồn tại không
            var url = $"{_userServiceUrl}/api/User/{userId}";
            
            _logger.LogInformation("🔍 Validating UserId {UserId} via User Service: {Url}", userId, url);
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ UserId {UserId} is valid", userId);
                return true;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("⚠️ UserId {UserId} not found in User Service", userId);
                return false;
            }
            
            // Nếu có lỗi khác (network, server error), log và return false để an toàn
            _logger.LogError("❌ Error validating UserId {UserId}: {StatusCode}", userId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            // Nếu không thể kết nối đến User Service, log warning nhưng không block booking
            // Vì JWT token đã được validate, nên UserId trong token đã đảm bảo là hợp lệ
            _logger.LogWarning(ex, "⚠️ Cannot connect to User Service to validate UserId {UserId}. Proceeding with booking as JWT token is already validated.", userId);
            
            // Return true vì JWT token đã validate user
            return true;
        }
    }
}

