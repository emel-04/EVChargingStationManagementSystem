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
        // FIX: Chặn ID không hợp lệ (Lỗi ITC_ST_14)
        if (id <= 0) return BadRequest("ID trạm sạc phải lớn hơn 0.");

        var station = await _stationService.GetStationByIdAsync(id);
        if (station == null)
        {
            return NotFound($"Không tìm thấy trạm sạc với ID: {id}");
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
        // BVA: Kiểm tra tọa độ hợp lệ
        if (latitude < -90 || latitude > 90) return BadRequest("Vĩ độ phải nằm trong khoảng [-90, 90].");
        if (longitude < -180 || longitude > 180) return BadRequest("Kinh độ phải nằm trong khoảng [-180, 180].");

        var stations = await _stationService.GetStationsByLocationAsync(latitude, longitude, radiusKm);
        return Ok(stations);
    }

    [HttpGet("city/{city}")]
    public async Task<ActionResult<IEnumerable<ChargingStation>>> GetStationsByCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city)) return BadRequest("Tên thành phố không được để trống.");
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
    [Authorize] // Bảo mật: Phải có Token (ITC_ST_12)
    public async Task<ActionResult<ChargingStation>> CreateStation(CreateStationRequest request)
    {
        // BVA: Chặn dữ liệu sai khi tạo mới
    
        if (string.IsNullOrEmpty(request.Name) || request.Name.Length < 5) 
            return BadRequest("Tên trạm phải có ít nhất 5 ký tự.");

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
    [Authorize] // FIX: Thêm Authorize để chặn Update khi không có Token (ITC_ST_12)
    public async Task<ActionResult<ChargingStation>> UpdateStation(int id, UpdateStationRequest request)
    {
        // 1. Chặn ID không hợp lệ
        if (id <= 0) return BadRequest("ID không hợp lệ.");

        // 2. BVA: Chặn giá âm (Lỗi ITC_ST_03) và giá vượt mức tối đa
        if (request.PricePerKwh < 0 || request.PricePerKwh > 100000) 
        {
            return BadRequest("Giá tiền không hợp lệ. Phải nằm trong khoảng 0 đến 100,000.");
        }
        // 3. BVA: Chặn tọa độ vượt ngưỡng (Lỗi ITC_ST_06, ITC_ST_07)
        if (request.Latitude < -90 || request.Latitude > 90) return BadRequest("Vĩ độ không hợp lệ (90.1).");
        if (request.Longitude < -180 || request.Longitude > 180) return BadRequest("Kinh độ không hợp lệ.");

        // 4. BVA: Chặn tên quá ngắn (Lỗi ITC_ST_08)
        if (string.IsNullOrEmpty(request.Name) || request.Name.Length < 5)
            return BadRequest("Tên trạm quá ngắn (Yêu cầu ít nhất 5 ký tự).");

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