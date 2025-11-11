using EVChargingStation.Shared.Models;
using EVChargingStation.UserService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVChargingStation.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehicleController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehicleController> _logger;

    public VehicleController(IVehicleService vehicleService, ILogger<VehicleController> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Vehicle>> GetVehicle(int id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        // Check if user owns the vehicle or is admin
        var userId = GetCurrentUserId();
        if (userId != vehicle.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return Ok(vehicle);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetUserVehicles(int userId)
    {
        // Check if user is accessing their own vehicles or is admin
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var vehicles = await _vehicleService.GetVehiclesByUserIdAsync(userId);
        return Ok(vehicles);
    }

    [HttpPost]
    public async Task<ActionResult<Vehicle>> CreateVehicle(CreateVehicleRequest request)
    {
        // Check if user is creating vehicle for themselves or is admin
        var currentUserId = GetCurrentUserId();
        if (currentUserId != request.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        try
        {
            var vehicle = await _vehicleService.CreateVehicleAsync(request);
            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Vehicle>> UpdateVehicle(int id, UpdateVehicleRequest request)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        // Check if user owns the vehicle or is admin
        var currentUserId = GetCurrentUserId();
        if (currentUserId != vehicle.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        try
        {
            var updatedVehicle = await _vehicleService.UpdateVehicleAsync(id, request);
            return Ok(updatedVehicle);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteVehicle(int id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        // Check if user owns the vehicle or is admin
        var currentUserId = GetCurrentUserId();
        if (currentUserId != vehicle.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var result = await _vehicleService.DeleteVehicleAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ActivateVehicle(int id)
    {
        var result = await _vehicleService.ActivateVehicleAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return Ok();
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeactivateVehicle(int id)
    {
        var result = await _vehicleService.DeactivateVehicleAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return Ok();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}


