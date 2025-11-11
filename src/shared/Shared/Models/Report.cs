using System.ComponentModel.DataAnnotations;

namespace EVChargingStation.Shared.Models;

public class Report
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string ReportType { get; set; } = string.Empty;
    
    public DateTime FromDate { get; set; }
    
    public DateTime ToDate { get; set; }
    
    [StringLength(50)]
    public string? StationId { get; set; }
    
    [StringLength(50)]
    public string? UserId { get; set; }
    
    public string? Data { get; set; } // JSON data
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int CreatedBy { get; set; }
}

public class StationUsageReport
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalEnergyConsumed { get; set; }
    public double AverageSessionDuration { get; set; }
    public DateTime ReportDate { get; set; }
}

public class UserUsageReport
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalEnergyConsumed { get; set; }
    public double AverageSessionDuration { get; set; }
    public DateTime ReportDate { get; set; }
}

public class RevenueReport
{
    public DateTime Date { get; set; }
    public decimal DailyRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal YearlyRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTransactionValue { get; set; }
}


