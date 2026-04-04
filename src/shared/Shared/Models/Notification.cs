using System.ComponentModel.DataAnnotations;

namespace EVChargingStation.Shared.Models;

public class Notification
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    public NotificationType Type { get; set; }
    
    public NotificationStatus Status { get; set; }
    
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReadAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}

public enum NotificationType
{
    BookingConfirmation = 1,
    BookingReminder = 2,
    ChargingStarted = 3,
    ChargingCompleted = 4,
    PaymentSuccess = 5,
    PaymentFailed = 6,
    StationMaintenance = 7,
    Promotional = 8,
    SystemAlert = 9
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4
}


