namespace EVChargingStation.NotificationService.Services;

public interface ISmsService
{
    Task<bool> SendSmsAsync(int userId, string message);
    Task<bool> SendSmsAsync(string phoneNumber, string message);
    Task<bool> SendBookingReminderAsync(int userId, string bookingNumber, DateTime startTime);
    Task<bool> SendChargingAlertAsync(int userId, string message);
}






