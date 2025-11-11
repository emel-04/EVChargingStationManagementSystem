using EVChargingStation.Shared.Models;
using EVChargingStation.StationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVChargingStation.StationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationController : ControllerBase
{
    private readonly IStationService _stationService;
    private readonly ILogger<StationController> _logger;

    public StationController(IStationService stationService, ILogger<StationController> logger)
    {
        _stationService = stationService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChargingStation>> GetStation(int id)
    {
        var station = await _stationService.GetStationByIdAsync(id);
        if (station == null)
        {
            return NotFound();
        }

        return Ok(station);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChargingStation>>> GetAllStations()
    {
        var stations = await _stationService.GetAllStationsAsync();
        return Ok(stations);
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<IEnumerable<ChargingStation>>> GetNearbyStations(
        [FromQuery] double latitude, 
        [FromQuery] double longitude, 
        [FromQuery] double radiusKm = 10)
    {
        var stations = await _stationService.GetStationsByLocationAsync(latitude, longitude, radiusKm);
        return Ok(stations);
    }

    [HttpGet("city/{city}")]
    public async Task<ActionResult<IEnumerable<ChargingStation>>> GetStationsByCity(string city)
    {
        var stations = await _stationService.GetStationsByCityAsync(city);
        return Ok(stations);
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<ChargingStation>>> GetStationsByStatus(StationStatus status)
    {
        var stations = await _stationService.GetStationsByStatusAsync(status);
        return Ok(stations);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ChargingStation>> CreateStation(CreateStationRequest request)
    {
        try
        {
            var station = await _stationService.CreateStationAsync(request);
            return CreatedAtAction(nameof(GetStation), new { id = station.Id }, station);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ChargingStation>> UpdateStation(int id, UpdateStationRequest request)
    {
        try
        {
            var station = await _stationService.UpdateStationAsync(id, request);
            return Ok(station);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteStation(int id)
    {
        var result = await _stationService.DeleteStationAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult> UpdateStationStatus(int id, UpdateStationStatusRequest request)
    {
        var result = await _stationService.UpdateStationStatusAsync(id, request.Status);
        if (!result)
        {
            return NotFound();
        }

        return Ok();
    }
}

public class UpdateStationStatusRequest
{
    public StationStatus Status { get; set; }
}






