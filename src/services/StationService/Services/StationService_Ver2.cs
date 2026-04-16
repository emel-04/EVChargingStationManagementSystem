using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.StationService.Services;

public class StationService : IStationService
{
    private readonly StationDbContext _context;
    private readonly ILogger<StationService> _logger;

    public StationService(StationDbContext context, ILogger<StationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChargingStation?> GetStationByIdAsync(int id)
    {
        return await _context.ChargingStations
            .Include(s => s.ChargingPoints)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<ChargingStation>> GetAllStationsAsync()
    {
        return await _context.ChargingStations
            .Include(s => s.ChargingPoints)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChargingStation>> GetStationsByLocationAsync(double latitude, double longitude, double radiusKm)
    {
        // Tính khoảng cách đơn giản (chưa dùng không gian địa lý)
        var stations = await _context.ChargingStations
            .Include(s => s.ChargingPoints)
            .ToListAsync();

        return stations.Where(s => CalculateDistance(latitude, longitude, s.Latitude, s.Longitude) <= radiusKm);
    }

    public async Task<IEnumerable<ChargingStation>> GetStationsByCityAsync(string city)
    {
        return await _context.ChargingStations
            .Include(s => s.ChargingPoints)
            .Where(s => s.City == city)
            .ToListAsync();
    }

    public async Task<ChargingStation> CreateStationAsync(CreateStationRequest request)
    {
        var station = new ChargingStation
        {
            Name = request.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            City = request.City,
            Province = request.Province,
            Status = request.Status,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChargingStations.Add(station);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Station created successfully: {Name}", station.Name);

        return station;
    }

    public async Task<ChargingStation> UpdateStationAsync(int id, UpdateStationRequest request)
{
    // Lấy entity trực tiếp từ DbContext (không dùng hàm GetStationByIdAsync)
    var station = await _context.ChargingStations.FirstOrDefaultAsync(s => s.Id == id);

    if (station == null)
        throw new ArgumentException("Station not found");

    // Gán lại giá trị nếu có trong request
    if (!string.IsNullOrEmpty(request.Name))
        station.Name = request.Name;

    if (!string.IsNullOrEmpty(request.Address))
        station.Address = request.Address;

    if (request.Latitude.HasValue)
        station.Latitude = request.Latitude.Value;

    if (request.Longitude.HasValue)
        station.Longitude = request.Longitude.Value;

    if (!string.IsNullOrEmpty(request.City))
        station.City = request.City;

    if (!string.IsNullOrEmpty(request.Province))
        station.Province = request.Province;

    if (request.Status.HasValue)
        station.Status = request.Status.Value;

    station.UpdatedAt = DateTime.UtcNow;

    try
    {
        // Đảm bảo EF nhận biết thay đổi
        _context.ChargingStations.Update(station);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✏️ Station updated successfully: {Name}", station.Name);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "❌ Failed to update station ID {Id}", id);
        throw new Exception("Error updating station in database");
    }

    return station;
}


    public async Task<bool> DeleteStationAsync(int id)
    {
        var station = await GetStationByIdAsync(id);
        if (station == null)
        {
            return false;
        }

        // Xóa thật khỏi cơ sở dữ liệu
        _context.ChargingStations.Remove(station);
        await _context.SaveChangesAsync();

        _logger.LogInformation("🗑️ Station deleted permanently: {Name}", station.Name);

        return true;
    }

    public async Task<bool> UpdateStationStatusAsync(int id, StationStatus status)
    {
        var station = await GetStationByIdAsync(id);
        if (station == null)
        {
            return false;
        }

        station.Status = status;
        station.UpdatedAt = DateTime.UtcNow;

        _context.Entry(station).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        _logger.LogInformation("🔄 Station status updated: {Name} - {Status}", station.Name, status);

        return true;
    }

    public async Task<IEnumerable<ChargingStation>> GetStationsByStatusAsync(StationStatus status)
    {
        return await _context.ChargingStations
            .Include(s => s.ChargingPoints)
            .Where(s => s.Status == status)
            .ToListAsync();
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Bán kính Trái Đất (km)
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}
