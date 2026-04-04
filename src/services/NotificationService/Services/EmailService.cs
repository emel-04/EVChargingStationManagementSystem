namespace EVChargingStation.NotificationService.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(int userId, string subject, string body)
    {
        try
        {
            // In a real application, you would:
            // 1. Get user email from UserService
            // 2. Use an email service like SendGrid, SMTP, etc.
            // 3. Send the actual email

            _logger.LogInformation("Email sent to user {UserId}: {Subject}", userId, subject);
            
            // Simulate email sending
            await Task.Delay(100);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string email, string subject, string body)
    {
        try
        {
            // In a real application, you would use an email service here
            _logger.LogInformation("Email sent to {Email}: {Subject}", email, subject);
            
            // Simulate email sending
            await Task.Delay(100);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendBookingConfirmationAsync(int userId, string bookingNumber)
    {
        var subject = "Xác nhận đặt chỗ sạc xe điện";
        var body = $@"
            <h2>Xác nhận đặt chỗ sạc xe điện</h2>
            <p>Xin chào,</p>
            <p>Đặt chỗ sạc xe điện của bạn đã được xác nhận thành công.</p>
            <p><strong>Mã đặt chỗ:</strong> {bookingNumber}</p>
            <p>Vui lòng đến trạm sạc đúng giờ và quét mã QR để bắt đầu sạc.</p>
            <p>Trân trọng,<br>Hệ thống quản lý trạm sạc xe điện</p>
        ";

        return await SendEmailAsync(userId, subject, body);
    }

    public async Task<bool> SendPaymentConfirmationAsync(int userId, string paymentNumber, decimal amount)
    {
        var subject = "Xác nhận thanh toán";
        var body = $@"
            <h2>Xác nhận thanh toán</h2>
            <p>Xin chào,</p>
            <p>Thanh toán của bạn đã được xử lý thành công.</p>
            <p><strong>Mã thanh toán:</strong> {paymentNumber}</p>
            <p><strong>Số tiền:</strong> {amount:N0} VND</p>
            <p>Trân trọng,<br>Hệ thống quản lý trạm sạc xe điện</p>
        ";

        return await SendEmailAsync(userId, subject, body);
    }

    public async Task<bool> SendChargingStartedAsync(int userId, string stationName)
    {
        var subject = "Bắt đầu sạc xe điện";
        var body = $@"
            <h2>Bắt đầu sạc xe điện</h2>
            <p>Xin chào,</p>
            <p>Phiên sạc xe điện của bạn đã bắt đầu tại trạm: <strong>{stationName}</strong></p>
            <p>Bạn sẽ nhận được thông báo khi quá trình sạc hoàn tất.</p>
            <p>Trân trọng,<br>Hệ thống quản lý trạm sạc xe điện</p>
        ";

        return await SendEmailAsync(userId, subject, body);
    }

    public async Task<bool> SendChargingCompletedAsync(int userId, string stationName, decimal energyConsumed, decimal totalCost)
    {
        var subject = "Hoàn tất sạc xe điện";
        var body = $@"
            <h2>Hoàn tất sạc xe điện</h2>
            <p>Xin chào,</p>
            <p>Phiên sạc xe điện của bạn đã hoàn tất tại trạm: <strong>{stationName}</strong></p>
            <p><strong>Năng lượng tiêu thụ:</strong> {energyConsumed:F2} kWh</p>
            <p><strong>Tổng chi phí:</strong> {totalCost:N0} VND</p>
            <p>Cảm ơn bạn đã sử dụng dịch vụ!</p>
            <p>Trân trọng,<br>Hệ thống quản lý trạm sạc xe điện</p>
        ";

        return await SendEmailAsync(userId, subject, body);
    }
}






