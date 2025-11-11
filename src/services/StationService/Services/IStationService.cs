using EVChargingStation.Shared.Models;

namespace EVChargingStation.StationService.Services;

public interface IStationService
{
    Task<ChargingStation?> GetStationByIdAsync(int id);
    Task<IEnumerable<ChargingStation>> GetAllStationsAsync();
    Task<IEnumerable<ChargingStation>> GetStationsByLocationAsync(double latitude, double longitude, double radiusKm);
    Task<IEnumerable<ChargingStation>> GetStationsByCityAsync(string city);
    Task<ChargingStation> CreateStationAsync(CreateStationRequest request);
    Task<ChargingStation> UpdateStationAsync(int id, UpdateStationRequest request);
    Task<bool> DeleteStationAsync(int id);
    Task<bool> UpdateStationStatusAsync(int id, StationStatus status);
    Task<IEnumerable<ChargingStation>> GetStationsByStatusAsync(StationStatus status);
}

public class CreateStationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public StationStatus Status { get; set; } = StationStatus.Online;
}

public class UpdateStationRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public StationStatus? Status { get; set; }
}


