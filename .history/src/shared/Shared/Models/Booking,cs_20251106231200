using System.ComponentModel.DataAnnotations;

namespace EVChargingStation.Shared.Models;

public class Booking
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    
    public int? ChargingPointId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string BookingNumber { get; set; } = string.Empty;
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
     
    public int StationId { get; set; }  // 👈 thêm dòng này

    public DateTime? ActualStartTime { get; set; }
    
    public DateTime? ActualEndTime { get; set; }
    
    public decimal? EnergyConsumed { get; set; } // kWh
    
    public decimal? TotalCost { get; set; }
    
    public BookingStatus Status { get; set; }
    
    public string? QRCode { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    // public Station? Station { get; set; } // ← Thêm navigation property
    public virtual ChargingPoint? ChargingPoint { get; set; } = null!;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum BookingStatus
{
    Pending = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    Expired = 6
}


