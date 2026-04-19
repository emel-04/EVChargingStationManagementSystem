using EVChargingStation.Shared.Models;
using EVChargingStation.ReportService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVChargingStation.ReportService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IReportService reportService, 
        IExcelExportService excelExportService,
        ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _excelExportService = excelExportService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<Report>> GetReport(int id)
    {
        var report = await _reportService.GetReportByIdAsync(id);
        if (report == null)
        {
            return NotFound();
        }

        return Ok(report);
    }

    [HttpGet("type/{reportType}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<IEnumerable<Report>>> GetReportsByType(string reportType)
    {
        var reports = await _reportService.GetReportsByTypeAsync(reportType);
        return Ok(reports);
    }

    [HttpGet("date-range")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<IEnumerable<Report>>> GetReportsByDateRange(
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var reports = await _reportService.GetReportsByDateRangeAsync(fromDate, toDate);
        return Ok(reports);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Report>> CreateReport(CreateReportRequest request)
    {
        try
        {
            var report = await _reportService.CreateReportAsync(request);
            return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("station-usage/{stationId}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<StationUsageReport>> GetStationUsageReport(
        int stationId, 
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService.GenerateStationUsageReportAsync(stationId, fromDate, toDate);
        return Ok(report);
    }

    [HttpGet("user-usage/{userId}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<UserUsageReport>> GetUserUsageReport(
        int userId, 
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService.GenerateUserUsageReportAsync(userId, fromDate, toDate);
        return Ok(report);
    }

    [HttpGet("revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RevenueReport>> GetRevenueReport(
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService.GenerateRevenueReportAsync(fromDate, toDate);
        return Ok(report);
    }

    [HttpGet("all-stations-usage")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<StationUsageReport>>> GetAllStationsUsageReport(
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var reports = await _reportService.GenerateAllStationsUsageReportAsync(fromDate, toDate);
        return Ok(reports);
    }

    [HttpGet("all-users-usage")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserUsageReport>>> GetAllUsersUsageReport(
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var reports = await _reportService.GenerateAllUsersUsageReportAsync(fromDate, toDate);
        return Ok(reports);
    }

    [HttpGet("station-usage/{stationId}/export")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<IActionResult> ExportStationUsageReport(
        int stationId, 
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService.GenerateStationUsageReportAsync(stationId, fromDate, toDate);
        var reports = new[] { report };
        var excelData = await _excelExportService.ExportStationUsageReportToExcelAsync(reports);
        
        var fileName = $"StationUsageReport_{stationId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("all-stations-usage/export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportAllStationsUsageReport(
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var reports = await _reportService.GenerateAllStationsUsageReportAsync(fromDate, toDate);
        var excelData = await _excelExportService.ExportStationUsageReportToExcelAsync(reports);
        
        var fileName = $"AllStationsUsageReport_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("all-users-usage/export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportAllUsersUsageReport(
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var reports = await _reportService.GenerateAllUsersUsageReportAsync(fromDate, toDate);
        var excelData = await _excelExportService.ExportUserUsageReportToExcelAsync(reports);
        
        var fileName = $"AllUsersUsageReport_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("revenue/export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportRevenueReport(
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        var report = await _reportService.GenerateRevenueReportAsync(fromDate, toDate);
        var excelData = await _excelExportService.ExportRevenueReportToExcelAsync(report);
        
        var fileName = $"RevenueReport_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}





