using EVChargingStation.Shared.Models;

namespace EVChargingStation.UserService.Services;

public interface IVehicleService
{
    Task<Vehicle?> GetVehicleByIdAsync(int id);
    Task<IEnumerable<Vehicle>> GetVehiclesByUserIdAsync(int userId);
    Task<Vehicle> CreateVehicleAsync(CreateVehicleRequest request);
    Task<Vehicle> UpdateVehicleAsync(int id, UpdateVehicleRequest request);
    Task<bool> DeleteVehicleAsync(int id);
    Task<bool> ActivateVehicleAsync(int id);
    Task<bool> DeactivateVehicleAsync(int id);
}

public class CreateVehicleRequest
{
    public int UserId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int BatteryCapacity { get; set; }
    public ChargingConnectorType ConnectorType { get; set; }
}

public class UpdateVehicleRequest
{
    public string? LicensePlate { get; set; }
    public string? Model { get; set; }
    public string? Brand { get; set; }
    public int? BatteryCapacity { get; set; }
    public ChargingConnectorType? ConnectorType { get; set; }
}


