using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.UserService.Services;

public class VehicleService : IVehicleService
{
    private readonly UserDbContext _context;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(UserDbContext context, ILogger<VehicleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        return await _context.Vehicles
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<Vehicle>> GetVehiclesByUserIdAsync(int userId)
    {
        return await _context.Vehicles
            .Where(v => v.UserId == userId && v.IsActive)
            .ToListAsync();
    }

    public async Task<Vehicle> CreateVehicleAsync(CreateVehicleRequest request)
    {
        // Check if user exists
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        // Check if license plate already exists
        var existingVehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.LicensePlate == request.LicensePlate);
        
        if (existingVehicle != null)
        {
            throw new InvalidOperationException("Vehicle with this license plate already exists");
        }

        var vehicle = new Vehicle
        {
            UserId = request.UserId,
            LicensePlate = request.LicensePlate,
            Model = request.Model,
            Brand = request.Brand,
            BatteryCapacity = request.BatteryCapacity,
            ConnectorType = request.ConnectorType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle created successfully: {LicensePlate}", vehicle.LicensePlate);

        return vehicle;
    }

    public async Task<Vehicle> UpdateVehicleAsync(int id, UpdateVehicleRequest request)
    {
        var vehicle = await GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            throw new ArgumentException("Vehicle not found");
        }

        if (!string.IsNullOrEmpty(request.LicensePlate))
        {
            // Check if license plate is already taken by another vehicle
            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.LicensePlate == request.LicensePlate && v.Id != id);
            
            if (existingVehicle != null)
            {
                throw new InvalidOperationException("License plate is already taken by another vehicle");
            }
            
            vehicle.LicensePlate = request.LicensePlate;
        }

        if (!string.IsNullOrEmpty(request.Model))
            vehicle.Model = request.Model;

        if (!string.IsNullOrEmpty(request.Brand))
            vehicle.Brand = request.Brand;

        if (request.BatteryCapacity.HasValue)
            vehicle.BatteryCapacity = request.BatteryCapacity.Value;

        if (request.ConnectorType.HasValue)
            vehicle.ConnectorType = request.ConnectorType.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle updated successfully: {LicensePlate}", vehicle.LicensePlate);

        return vehicle;
    }

    public async Task<bool> DeleteVehicleAsync(int id)
    {
        var vehicle = await GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            return false;
        }

        vehicle.IsActive = false;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle deactivated: {LicensePlate}", vehicle.LicensePlate);

        return true;
    }

    public async Task<bool> ActivateVehicleAsync(int id)
    {
        var vehicle = await GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            return false;
        }

        vehicle.IsActive = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle activated: {LicensePlate}", vehicle.LicensePlate);

        return true;
    }

    public async Task<bool> DeactivateVehicleAsync(int id)
    {
        var vehicle = await GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            return false;
        }

        vehicle.IsActive = false;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Vehicle deactivated: {LicensePlate}", vehicle.LicensePlate);

        return true;
    }
}


