using EVChargingStation.Shared.Models;
using EVChargingStation.StationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVChargingStation.StationService.Controllers;

[ApiController]
[Route("api/Station_Ver1")] // Đổi route để không bị trùng lặp với file chuẩn
public class StationController_Ver1 : ControllerBase
{
    private readonly IStationService _stationService;
    private readonly ILogger<StationController_Ver1> _logger;

    public StationController_Ver1(IStationService stationService, ILogger<StationController_Ver1> logger)
    {
        _stationService = stationService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChargingStation>> GetStation(int id)
    {
        // VER 1 LỖI: Code non, quên chặn ID <= 0
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
        // VER 1 LỖI: Không kiểm tra giá trị biên của tọa độ (Latitude, Longitude)
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
    // VER 1 LỖI: Quên gắn thẻ [Authorize], ai cũng có thể tạo trạm sạc (Lỗi bảo mật)
    public async Task<ActionResult<ChargingStation>> CreateStation(CreateStationRequest request)
    {
        // VER 1 LỖI: Không chặn tên trạm quá ngắn, cho phép rác lọt vào DB
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
    // VER 1 LỖI: Bỏ quên [Authorize], cho phép sửa data vô tội vạ
    public async Task<ActionResult<ChargingStation>> UpdateStation(int id, UpdateStationRequest request)
    {
        // VER 1 LỖI KINH ĐIỂN: Hàm Update bị trống trơn phần Validation (Kiểm duyệt).
        // - Không chặn ID <= 0
        // - Không chặn Giá bị âm (Lỗi Test Case BVA sẽ văng 500 ở đây)
        // - Không chặn Tọa độ ảo
        // - Không chặn Tên rỗng

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