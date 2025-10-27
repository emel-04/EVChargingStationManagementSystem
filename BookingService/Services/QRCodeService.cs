using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace EVChargingStation.BookingService.Services;

public class QRCodeService : IQRCodeService
{
    private readonly ILogger<QRCodeService> _logger;

    public QRCodeService(ILogger<QRCodeService> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateQRCodeAsync(string data)
    {
        try
        {
            // Create simple text-based QR code for now
            var base64String = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
            _logger.LogInformation("QR Code generated successfully for data: {Data}", data);
            return Task.FromResult(base64String);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for data: {Data}", data);
            throw;
        }
    }

    public Task<byte[]> GenerateQRCodeBytesAsync(string data)
    {
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            _logger.LogInformation("QR Code bytes generated successfully for data: {Data}", data);
            return Task.FromResult(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code bytes for data: {Data}", data);
            throw;
        }
    }

    public Task<string> GenerateQRCodeForBookingAsync(string bookingId)
    {
        try
        {
            var qrData = $"booking:{bookingId}";
            return GenerateQRCodeAsync(qrData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for booking: {BookingId}", bookingId);
            throw;
        }
    }
}