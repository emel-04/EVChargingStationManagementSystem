using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EVChargingStation.ReportService.Services;

public class ReportService : IReportService
{
    private readonly ReportDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ReportDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Report?> GetReportByIdAsync(int id)
    {
        return await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Report>> GetReportsByTypeAsync(string reportType)
    {
        return await _context.Reports
            .Where(r => r.ReportType == reportType)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Report>> GetReportsByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _context.Reports
            .Where(r => r.FromDate >= fromDate && r.ToDate <= toDate)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Report> CreateReportAsync(CreateReportRequest request)
    {
        var report = new Report
        {
            Title = request.Title,
            ReportType = request.ReportType,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            StationId = request.StationId,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report created: {Title}", report.Title);

        return report;
    }

    public async Task<StationUsageReport> GenerateStationUsageReportAsync(int stationId, DateTime fromDate, DateTime toDate)
    {
        // In a real application, you would query the actual data from other services
        // For now, we'll generate mock data
        
        var report = new StationUsageReport
        {
            StationId = stationId,
            StationName = $"Station {stationId}",
            TotalBookings = Random.Shared.Next(50, 200),
            TotalRevenue = Random.Shared.Next(1000000, 5000000),
            TotalEnergyConsumed = Random.Shared.Next(1000, 5000),
            AverageSessionDuration = Random.Shared.NextDouble() * 120 + 30, // 30-150 minutes
            ReportDate = DateTime.UtcNow
        };

        // Save report to database
        var reportData = JsonSerializer.Serialize(report);
        var dbReport = new Report
        {
            Title = $"Station Usage Report - Station {stationId}",
            ReportType = "StationUsage",
            FromDate = fromDate,
            ToDate = toDate,
            StationId = stationId.ToString(),
            Data = reportData,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1 // This should come from the authenticated user
        };

        _context.Reports.Add(dbReport);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Station usage report generated for station {StationId}", stationId);

        return report;
    }

    public async Task<UserUsageReport> GenerateUserUsageReportAsync(int userId, DateTime fromDate, DateTime toDate)
    {
        // In a real application, you would query the actual data from other services
        // For now, we'll generate mock data
        
        var report = new UserUsageReport
        {
            UserId = userId,
            UserName = $"User {userId}",
            TotalBookings = Random.Shared.Next(5, 50),
            TotalSpent = Random.Shared.Next(500000, 2000000),
            TotalEnergyConsumed = Random.Shared.Next(100, 1000),
            AverageSessionDuration = Random.Shared.NextDouble() * 120 + 30, // 30-150 minutes
            ReportDate = DateTime.UtcNow
        };

        // Save report to database
        var reportData = JsonSerializer.Serialize(report);
        var dbReport = new Report
        {
            Title = $"User Usage Report - User {userId}",
            ReportType = "UserUsage",
            FromDate = fromDate,
            ToDate = toDate,
            UserId = userId.ToString(),
            Data = reportData,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1 // This should come from the authenticated user
        };

        _context.Reports.Add(dbReport);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User usage report generated for user {UserId}", userId);

        return report;
    }

    public async Task<RevenueReport> GenerateRevenueReportAsync(DateTime fromDate, DateTime toDate)
    {
        // In a real application, you would query the actual data from other services
        // For now, we'll generate mock data
        
        var report = new RevenueReport
        {
            Date = DateTime.UtcNow,
            DailyRevenue = Random.Shared.Next(1000000, 5000000),
            MonthlyRevenue = Random.Shared.Next(30000000, 150000000),
            YearlyRevenue = Random.Shared.Next(360000000, 1800000000),
            TotalTransactions = Random.Shared.Next(100, 1000),
            AverageTransactionValue = Random.Shared.Next(50000, 200000)
        };

        // Save report to database
        var reportData = JsonSerializer.Serialize(report);
        var dbReport = new Report
        {
            Title = "Revenue Report",
            ReportType = "Revenue",
            FromDate = fromDate,
            ToDate = toDate,
            Data = reportData,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1 // This should come from the authenticated user
        };

        _context.Reports.Add(dbReport);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Revenue report generated");

        return report;
    }

    public async Task<IEnumerable<StationUsageReport>> GenerateAllStationsUsageReportAsync(DateTime fromDate, DateTime toDate)
    {
        // In a real application, you would query all stations and generate reports for each
        // For now, we'll generate mock data for 5 stations
        
        var reports = new List<StationUsageReport>();
        
        for (int i = 1; i <= 5; i++)
        {
            var report = await GenerateStationUsageReportAsync(i, fromDate, toDate);
            reports.Add(report);
        }

        return reports;
    }

    public async Task<IEnumerable<UserUsageReport>> GenerateAllUsersUsageReportAsync(DateTime fromDate, DateTime toDate)
    {
        // In a real application, you would query all users and generate reports for each
        // For now, we'll generate mock data for 10 users
        
        var reports = new List<UserUsageReport>();
        
        for (int i = 1; i <= 10; i++)
        {
            var report = await GenerateUserUsageReportAsync(i, fromDate, toDate);
            reports.Add(report);
        }

        return reports;
    }
}


