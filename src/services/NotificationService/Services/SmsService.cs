namespace EVChargingStation.NotificationService.Services;

public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(int userId, string message)
    {
        try
        {
            // In a real application, you would:
            // 1. Get user phone number from UserService
            // 2. Use an SMS service like Twilio, AWS SNS, etc.
            // 3. Send the actual SMS

            _logger.LogInformation("SMS sent to user {UserId}: {Message}", userId, message);
            
            // Simulate SMS sending
            await Task.Delay(100);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            // In a real application, you would use an SMS service here
            _logger.LogInformation("SMS sent to {PhoneNumber}: {Message}", phoneNumber, message);
            
            // Simulate SMS sending
            await Task.Delay(100);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> SendBookingReminderAsync(int userId, string bookingNumber, DateTime startTime)
    {
        var message = $"Nhắc nhở: Bạn có lịch sạc xe điện tại {startTime:HH:mm dd/MM/yyyy}. Mã đặt chỗ: {bookingNumber}";
        return await SendSmsAsync(userId, message);
    }

    public async Task<bool> SendChargingAlertAsync(int userId, string message)
    {
        var alertMessage = $"Cảnh báo sạc xe: {message}";
        return await SendSmsAsync(userId, alertMessage);
    }
}






