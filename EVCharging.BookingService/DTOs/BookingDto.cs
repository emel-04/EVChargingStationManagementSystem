namespace EVCharging.BookingService.DTOs;

public class BookingDto
{
    public long BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public long UserId { get; set; }
    public long ChargerId { get; set; }
    public long StationId { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? EstimatedDuration { get; set; }
    public int? ActualDuration { get; set; }
    public decimal? EnergyConsumed { get; set; }
    public string? QRCode { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
