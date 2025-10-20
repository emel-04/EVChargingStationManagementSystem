namespace EVCharging.BookingService.DTOs;

public class CreateBookingRequest
{
    public long ChargerId { get; set; }
    public long StationId { get; set; }
    public DateTime StartTime { get; set; }
    public int EstimatedDuration { get; set; } // minutes
    public string? Notes { get; set; }
}
