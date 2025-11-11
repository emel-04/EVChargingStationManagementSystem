using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.StationService.Services;

public class ChargingPointService : IChargingPointService
{
    private readonly StationDbContext _context;

    public ChargingPointService(StationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ChargingPoint>> GetAllChargingPointsAsync()
    {
        return await _context.ChargingPoints
            .Include(cp => cp.Station)
            .ToListAsync();
    }

    public async Task<ChargingPoint?> GetChargingPointByIdAsync(int id)
    {
        return await _context.ChargingPoints
            .Include(cp => cp.Station)
            .FirstOrDefaultAsync(cp => cp.Id == id);
    }

    public async Task<IEnumerable<ChargingPoint>> GetChargingPointsByStationIdAsync(int stationId)
    {
        return await _context.ChargingPoints
            .Where(cp => cp.StationId == stationId)
            .Include(cp => cp.Station)
            .ToListAsync();
    }

    public async Task<ChargingPoint> CreateChargingPointAsync(CreateChargingPointRequest request)
    {
        var chargingPoint = new ChargingPoint
        {
            StationId = request.StationId,
            PointNumber = request.PointNumber,
            ConnectorType = request.ConnectorType,
            MaxPower = request.MaxPower,
            PricePerKwh = request.PricePerKwh,
            PricePerHour = request.PricePerHour,
            Status = PointStatus.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ChargingPoints.Add(chargingPoint);
        await _context.SaveChangesAsync();
        return chargingPoint;
    }

    public async Task<ChargingPoint> UpdateChargingPointAsync(int id, UpdateChargingPointRequest request)
    {
        var existingChargingPoint = await _context.ChargingPoints.FindAsync(id);
        if (existingChargingPoint == null)
            throw new ArgumentException("Charging point not found");

        if (request.PointNumber != null)
            existingChargingPoint.PointNumber = request.PointNumber;
        if (request.ConnectorType.HasValue)
            existingChargingPoint.ConnectorType = request.ConnectorType.Value;
        if (request.MaxPower.HasValue)
            existingChargingPoint.MaxPower = request.MaxPower.Value;
        if (request.PricePerKwh.HasValue)
            existingChargingPoint.PricePerKwh = request.PricePerKwh.Value;
        if (request.PricePerHour.HasValue)
            existingChargingPoint.PricePerHour = request.PricePerHour.Value;
        
        existingChargingPoint.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingChargingPoint;
    }

    public async Task<bool> DeleteChargingPointAsync(int id)
    {
        var chargingPoint = await _context.ChargingPoints.FindAsync(id);
        if (chargingPoint == null)
            return false;

        _context.ChargingPoints.Remove(chargingPoint);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateChargingPointStatusAsync(int id, PointStatus status)
    {
        var chargingPoint = await _context.ChargingPoints.FindAsync(id);
        if (chargingPoint == null)
            return false;

        chargingPoint.Status = status;
        chargingPoint.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ChargingPoint>> GetAvailableChargingPointsAsync()
    {
        return await _context.ChargingPoints
            .Where(cp => cp.Status == PointStatus.Available)
            .Include(cp => cp.Station)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChargingPoint>> GetChargingPointsByConnectorTypeAsync(ChargingConnectorType connectorType)
    {
        return await _context.ChargingPoints
            .Where(cp => cp.ConnectorType == connectorType)
            .Include(cp => cp.Station)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChargingPoint>> GetChargingPointsByStatusAsync(PointStatus status)
    {
        return await _context.ChargingPoints
            .Where(cp => cp.Status == status)
            .Include(cp => cp.Station)
            .ToListAsync();
    }
}