using ClosedXML.Excel;
using EVChargingStation.Shared.Models;

namespace EVChargingStation.ReportService.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly ILogger<ExcelExportService> _logger;

    public ExcelExportService(ILogger<ExcelExportService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ExportStationUsageReportToExcelAsync(IEnumerable<object> reports)
    {
        return await ExportGeneralReportToExcelAsync(reports, "Station Usage Report");
    }

    public async Task<byte[]> ExportUserUsageReportToExcelAsync(IEnumerable<object> reports)
    {
        return await ExportGeneralReportToExcelAsync(reports, "User Usage Report");
    }

    public async Task<byte[]> ExportRevenueReportToExcelAsync(object report)
    {
        return await ExportGeneralReportToExcelAsync(new[] { report }, "Revenue Report");
    }

    public async Task<byte[]> ExportGeneralReportToExcelAsync<T>(IEnumerable<T> data, string sheetName)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            // Add headers
            worksheet.Cell(1, 1).Value = "Data";
            worksheet.Cell(1, 2).Value = "Type";
            worksheet.Cell(1, 3).Value = "Export Date";

            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item?.ToString() ?? "N/A";
                worksheet.Cell(row, 2).Value = typeof(T).Name;
                worksheet.Cell(row, 3).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return await Task.FromResult(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting {SheetName} to Excel", sheetName);
            throw;
        }
    }
}