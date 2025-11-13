namespace EVChargingStation.BookingService.Services;

public interface IQRCodeService
{
    Task<string> GenerateQRCodeAsync(string data);
    Task<string> GenerateQRCodeForBookingAsync(string bookingNumber);
}






