using EVChargingStation.Shared.Models;

namespace EVChargingStation.NotificationService.Services;

public interface INotificationService
{
    Task<Notification?> GetNotificationByIdAsync(int id);
    Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(int userId);
    Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(int userId);
    Task<Notification> CreateNotificationAsync(CreateNotificationRequest request);
    Task<bool> MarkAsReadAsync(int id);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<bool> DeleteNotificationAsync(int id);
    Task SendNotificationAsync(int userId, string title, string message, NotificationType type);
    Task SendBulkNotificationAsync(IEnumerable<int> userIds, string title, string message, NotificationType type);
}

public class CreateNotificationRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
}






