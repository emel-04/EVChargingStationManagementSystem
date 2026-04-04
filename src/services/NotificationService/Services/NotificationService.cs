using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.NotificationService.Services;

public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationDbContext context, 
        IEmailService emailService, 
        ISmsService smsService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<Notification?> GetNotificationByIdAsync(int id)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Notification> CreateNotificationAsync(CreateNotificationRequest request)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Status = NotificationStatus.Pending,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send notification via appropriate channels
        await SendNotificationToChannelsAsync(notification);

        _logger.LogInformation("Notification created: {Title} for user {UserId}", 
            notification.Title, notification.UserId);

        return notification;
    }

    public async Task<bool> MarkAsReadAsync(int id)
    {
        var notification = await GetNotificationByIdAsync(id);
        if (notification == null)
        {
            return false;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification marked as read: {Id}", id);

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("All notifications marked as read for user: {UserId}", userId);

        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int id)
    {
        var notification = await GetNotificationByIdAsync(id);
        if (notification == null)
        {
            return false;
        }

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification deleted: {Id}", id);

        return true;
    }

    public async Task SendNotificationAsync(int userId, string title, string message, NotificationType type)
    {
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type
        };

        await CreateNotificationAsync(request);
    }

    public async Task SendBulkNotificationAsync(IEnumerable<int> userIds, string title, string message, NotificationType type)
    {
        var tasks = userIds.Select(userId => SendNotificationAsync(userId, title, message, type));
        await Task.WhenAll(tasks);

        _logger.LogInformation("Bulk notification sent to {Count} users", userIds.Count());
    }

    private async Task SendNotificationToChannelsAsync(Notification notification)
    {
        try
        {
            // Send email for important notifications
            if (notification.Type == NotificationType.BookingConfirmation ||
                notification.Type == NotificationType.PaymentSuccess ||
                notification.Type == NotificationType.SystemAlert)
            {
                await _emailService.SendEmailAsync(notification.UserId, notification.Title, notification.Message);
            }

            // Send SMS for urgent notifications
            if (notification.Type == NotificationType.ChargingStarted ||
                notification.Type == NotificationType.ChargingCompleted ||
                notification.Type == NotificationType.SystemAlert)
            {
                await _smsService.SendSmsAsync(notification.UserId, notification.Message);
            }

            notification.Status = NotificationStatus.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification {Id} to channels", notification.Id);
            notification.Status = NotificationStatus.Failed;
        }

        await _context.SaveChangesAsync();
    }
}


