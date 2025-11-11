using EVChargingStation.Shared.Models;
using EVChargingStation.StationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVChargingStation.StationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChargingPointController : ControllerBase
{
    private readonly IChargingPointService _chargingPointService;
    private readonly ILogger<ChargingPointController> _logger;

    public ChargingPointController(IChargingPointService chargingPointService, ILogger<ChargingPointController> logger)
    {
        _chargingPointService = chargingPointService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChargingPoint>> GetChargingPoint(int id)
    {
        var chargingPoint = await _chargingPointService.GetChargingPointByIdAsync(id);
        if (chargingPoint == null)
        {
            return NotFound();
        }

        return Ok(chargingPoint);
    }

    [HttpGet("station/{stationId}")]
    public async Task<ActionResult<IEnumerable<ChargingPoint>>> GetChargingPointsByStation(int stationId)
    {
        var chargingPoints = await _chargingPointService.GetChargingPointsByStationIdAsync(stationId);
        return Ok(chargingPoints);
    }

    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<ChargingPoint>>> GetAvailableChargingPoints()
    {
        var chargingPoints = await _chargingPointService.GetAvailableChargingPointsAsync();
        return Ok(chargingPoints);
    }

    [HttpGet("connector-type/{connectorType}")]
    public async Task<ActionResult<IEnumerable<ChargingPoint>>> GetChargingPointsByConnectorType(ChargingConnectorType connectorType)
    {
        var chargingPoints = await _chargingPointService.GetChargingPointsByConnectorTypeAsync(connectorType);
        return Ok(chargingPoints);
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<ChargingPoint>>> GetChargingPointsByStatus(PointStatus status)
    {
        var chargingPoints = await _chargingPointService.GetChargingPointsByStatusAsync(status);
        return Ok(chargingPoints);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ChargingPoint>> CreateChargingPoint(CreateChargingPointRequest request)
    {
        try
        {
            var chargingPoint = await _chargingPointService.CreateChargingPointAsync(request);
            return CreatedAtAction(nameof(GetChargingPoint), new { id = chargingPoint.Id }, chargingPoint);
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
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ChargingPoint>> UpdateChargingPoint(int id, UpdateChargingPointRequest request)
    {
        try
        {
            var chargingPoint = await _chargingPointService.UpdateChargingPointAsync(id, request);
            return Ok(chargingPoint);
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
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteChargingPoint(int id)
    {
        var result = await _chargingPointService.DeleteChargingPointAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult> UpdateChargingPointStatus(int id, UpdateChargingPointStatusRequest request)
    {
        var result = await _chargingPointService.UpdateChargingPointStatusAsync(id, request.Status);
        if (!result)
        {
            return NotFound();
        }

        return Ok();
    }
}

public class UpdateChargingPointStatusRequest
{
    public PointStatus Status { get; set; }
}






