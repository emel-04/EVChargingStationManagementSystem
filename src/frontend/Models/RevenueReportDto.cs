namespace EVChargingStation.Web.Models
{
    public class RevenueReportDto
    {
        public DateTime Date { get; set; }
        public decimal DailyRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal YearlyRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
    }
}