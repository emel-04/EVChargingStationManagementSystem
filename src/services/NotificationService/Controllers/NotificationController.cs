using EVChargingStation.Shared.Models;
using EVChargingStation.NotificationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EVChargingStation.NotificationService.Hubs;
using System.Security.Claims;

namespace EVChargingStation.NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService, 
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Notification>> GetNotification(int id)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(id);
        if (notification == null)
        {
            return NotFound();
        }

        // Check if user owns the notification or is admin/staff
        var userId = GetCurrentUserId();
        if (userId != notification.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        return Ok(notification);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications(int userId)
    {
        // Check if user is accessing their own notifications or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId);
        return Ok(notifications);
    }

    [HttpGet("user/{userId}/unread")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetUnreadNotifications(int userId)
    {
        // Check if user is accessing their own notifications or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var notifications = await _notificationService.GetUnreadNotificationsByUserIdAsync(userId);
        return Ok(notifications);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Notification>> CreateNotification(CreateNotificationRequest request)
    {
        try
        {
            var notification = await _notificationService.CreateNotificationAsync(request);
            
            // Send real-time notification via SignalR
            await _hubContext.Clients.Group($"User_{request.UserId}").SendAsync("ReceiveNotification", notification);
            
            return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("send")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SendNotification(SendNotificationRequest request)
    {
        try
        {
            await _notificationService.SendNotificationAsync(request.UserId, request.Title, request.Message, request.Type);
            
            // Send real-time notification via SignalR
            await _hubContext.Clients.Group($"User_{request.UserId}").SendAsync("ReceiveNotification", new
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow
            });
            
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("bulk-send")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SendBulkNotification(SendBulkNotificationRequest request)
    {
        try
        {
            await _notificationService.SendBulkNotificationAsync(request.UserIds, request.Title, request.Message, request.Type);
            
            // Send real-time notifications via SignalR
            var tasks = request.UserIds.Select(userId => 
                _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new
                {
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type,
                    CreatedAt = DateTime.UtcNow
                }));
            await Task.WhenAll(tasks);
            
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult> MarkAsRead(int id)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(id);
        if (notification == null)
        {
            return NotFound();
        }

        // Check if user owns the notification or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != notification.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var result = await _notificationService.MarkAsReadAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return Ok();
    }

    [HttpPut("user/{userId}/read-all")]
    public async Task<ActionResult> MarkAllAsRead(int userId)
    {
        // Check if user is marking their own notifications or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var result = await _notificationService.MarkAllAsReadAsync(userId);
        if (!result)
        {
            return BadRequest("Failed to mark notifications as read");
        }

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(int id)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(id);
        if (notification == null)
        {
            return NotFound();
        }

        // Check if user owns the notification or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != notification.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var result = await _notificationService.DeleteNotificationAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

public class SendNotificationRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
}

public class SendBulkNotificationRequest
{
    public IEnumerable<int> UserIds { get; set; } = new List<int>();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
}





