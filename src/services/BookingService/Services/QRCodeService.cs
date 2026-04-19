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
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);
            var base64String = Convert.ToBase64String(qrCodeImage);

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
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var bytes = qrCode.GetGraphic(20);

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