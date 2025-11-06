using System.ComponentModel.DataAnnotations;

namespace EVChargingStation.Shared.Models;

public class ChargingStation
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;
    
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    [StringLength(50)]
    public string? City { get; set; }
    
    [StringLength(50)]
    public string? Province { get; set; }
    
    public StationStatus Status { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<ChargingPoint> ChargingPoints { get; set; } = new List<ChargingPoint>();
}

public class ChargingPoint
{
    public int Id { get; set; }
    
    [Required]
    public int StationId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string PointNumber { get; set; } = string.Empty;
    
    public ChargingConnectorType ConnectorType { get; set; }
    
    public int MaxPower { get; set; } // kW
    
    public decimal PricePerKwh { get; set; }
    
    public decimal PricePerHour { get; set; }
    
    public PointStatus Status { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ChargingStation Station { get; set; } = null!;
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public enum StationStatus
{
    Online = 1,
    Offline = 2,
    Maintenance = 3
}

public enum PointStatus
{
    Available = 1,
    Occupied = 2,
    OutOfOrder = 3,
    Maintenance = 4
}


