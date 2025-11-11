using EVChargingStation.Shared.Models;

namespace EVChargingStation.StationService.Services;

public interface IChargingPointService
{
    Task<ChargingPoint?> GetChargingPointByIdAsync(int id);
    Task<IEnumerable<ChargingPoint>> GetChargingPointsByStationIdAsync(int stationId);
    Task<IEnumerable<ChargingPoint>> GetAvailableChargingPointsAsync();
    Task<IEnumerable<ChargingPoint>> GetChargingPointsByConnectorTypeAsync(ChargingConnectorType connectorType);
    Task<ChargingPoint> CreateChargingPointAsync(CreateChargingPointRequest request);
    Task<ChargingPoint> UpdateChargingPointAsync(int id, UpdateChargingPointRequest request);
    Task<bool> DeleteChargingPointAsync(int id);
    Task<bool> UpdateChargingPointStatusAsync(int id, PointStatus status);
    Task<IEnumerable<ChargingPoint>> GetChargingPointsByStatusAsync(PointStatus status);
}

public class CreateChargingPointRequest
{
    public int StationId { get; set; }
    public string PointNumber { get; set; } = string.Empty;
    public ChargingConnectorType ConnectorType { get; set; }
    public int MaxPower { get; set; }
    public decimal PricePerKwh { get; set; }
    public decimal PricePerHour { get; set; }
    public PointStatus Status { get; set; } = PointStatus.Available;
}

public class UpdateChargingPointRequest
{
    public string? PointNumber { get; set; }
    public ChargingConnectorType? ConnectorType { get; set; }
    public int? MaxPower { get; set; }
    public decimal? PricePerKwh { get; set; }
    public decimal? PricePerHour { get; set; }
    public PointStatus? Status { get; set; }
}


