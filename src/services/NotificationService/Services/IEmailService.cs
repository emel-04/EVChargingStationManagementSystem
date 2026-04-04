namespace EVChargingStation.NotificationService.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(int userId, string subject, string body);
    Task<bool> SendEmailAsync(string email, string subject, string body);
    Task<bool> SendBookingConfirmationAsync(int userId, string bookingNumber);
    Task<bool> SendPaymentConfirmationAsync(int userId, string paymentNumber, decimal amount);
    Task<bool> SendChargingStartedAsync(int userId, string stationName);
    Task<bool> SendChargingCompletedAsync(int userId, string stationName, decimal energyConsumed, decimal totalCost);
}






