using EVChargingStation.Shared.Models;

namespace EVChargingStation.ReportService.Services;

public interface IReportService
{
    Task<Report?> GetReportByIdAsync(int id);
    Task<IEnumerable<Report>> GetReportsByTypeAsync(string reportType);
    Task<IEnumerable<Report>> GetReportsByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<Report> CreateReportAsync(CreateReportRequest request);
    Task<StationUsageReport> GenerateStationUsageReportAsync(int stationId, DateTime fromDate, DateTime toDate);
    Task<UserUsageReport> GenerateUserUsageReportAsync(int userId, DateTime fromDate, DateTime toDate);
    Task<RevenueReport> GenerateRevenueReportAsync(DateTime fromDate, DateTime toDate);
    Task<IEnumerable<StationUsageReport>> GenerateAllStationsUsageReportAsync(DateTime fromDate, DateTime toDate);
    Task<IEnumerable<UserUsageReport>> GenerateAllUsersUsageReportAsync(DateTime fromDate, DateTime toDate);
}

public class CreateReportRequest
{
    public string Title { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? StationId { get; set; }
    public string? UserId { get; set; }
    public int CreatedBy { get; set; }
}





