namespace EVChargingStation.ReportService.Services;

public interface IExcelExportService
{
    Task<byte[]> ExportStationUsageReportToExcelAsync(IEnumerable<object> reports);
    Task<byte[]> ExportUserUsageReportToExcelAsync(IEnumerable<object> reports);
    Task<byte[]> ExportRevenueReportToExcelAsync(object report);
    Task<byte[]> ExportGeneralReportToExcelAsync<T>(IEnumerable<T> data, string sheetName);
}



