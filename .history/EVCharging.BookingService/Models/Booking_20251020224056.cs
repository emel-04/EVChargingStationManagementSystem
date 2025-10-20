namespace EVCharging.BookingService.Models;

public class Booking
{
    public long BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public long UserId { get; set; }
    public long ChargerId { get; set; }
    public long StationId { get; set; }
    public string BookingStatus { get; set; } = "Pending"; // Pending, Confirmed, InProgress, Completed, Cancelled
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? EstimatedDuration { get; set; } // minutes
    public int? ActualDuration { get; set; } // minutes
    public decimal? EnergyConsumed { get; set; } // kWh
    public string? QRCode { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
