using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lwesihlanu.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;

public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var reports = _context.Reports.Include(r => r.UserReport).ToList();
        return View(reports);
    }

    public IActionResult Create()
    {
        var columns = _context.ReportColumns.ToList();
        return View(columns); // Ensure your view is strongly typed to IEnumerable<ReportColumn>
    }

    public async Task<IActionResult> SaveReportConfiguration(string reportName, string description, string[] selectedColumnIds, string format)
    {
        // Convert selectedColumnIds to a list of column names
        var selectedColumns = _context.ReportColumns
                                      .Where(rc => selectedColumnIds.Contains(rc.ColumnId.ToString()))
                                      .Select(rc => rc.Name)
                                      .ToList();

        var selectedColumnsString = string.Join(",", selectedColumns);
        var queryJson = JsonConvert.SerializeObject(new { SelectedColumns = selectedColumns });

        // Check if the query already exists
        var existingQuery = _context.UserReports
                                    .FirstOrDefault(ur => ur.QueryText == queryJson);

        int queryId;
        if (existingQuery == null)
        {
            // Save new query to UserReport table
            var userReport = new UserReport
            {
                SelectedColumns = selectedColumnsString,
                QueryText = queryJson,
                Filters = "tttttt", // Add filter logic if needed
                CreatedDate = DateTime.Now
            };

            _context.UserReports.Add(userReport);
            await _context.SaveChangesAsync();
            queryId = userReport.UserReportId;
        }
        else
        {
            // Use the existing query ID
            queryId = existingQuery.UserReportId;
        }

        // Save report to Report table
        var report = new Report
        {
            Name = reportName,
            Description = description,
            SelectedColumns = selectedColumnsString,
            Format = format,
            CreatedDate = DateTime.Now,
            QueryId = queryId
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        // Save related ReportColumns
        foreach (var columnId in selectedColumnIds)
        {
            var reportColumn = new ReportColumn
            {
                ReportId = report.ReportId,
                IsSelected = true, // Assuming selected columns are marked as true
                Name = _context.ReportColumns.FirstOrDefault(rc => rc.ColumnId == int.Parse(columnId))?.Name
            };

            if (reportColumn.Name != null)
            {
                _context.ReportColumns.Add(reportColumn);
            }
        }

        await _context.SaveChangesAsync();

        return await DownloadReport(report.ReportId, format);
    }


    // GET: Reports/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var report = await _context.Reports
            .Include(r => r.ReportColumns)
            .Include(r => r.UserReport)
            .FirstOrDefaultAsync(m => m.ReportId == id);

        if (report == null)
        {
            return NotFound();
        }

        return View(report);
    }

    // POST: Reports/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var report = await _context.Reports
            .Include(r => r.ReportColumns)
            .FirstOrDefaultAsync(m => m.ReportId == id);

        if (report == null)
        {
            return NotFound();
        }

        // Remove associated ReportColumns
        var reportColumns = _context.ReportColumns.Where(rc => rc.ReportId == report.ReportId);
        _context.ReportColumns.RemoveRange(reportColumns);

        _context.Reports.Remove(report);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Action to Run and Download the report
    public async Task<IActionResult> Run(int id, string format)
    {
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.ReportId == id);

        if (report == null)
        {
            return NotFound();
        }

        var selectedColumns = report.SelectedColumns.Split(',');

        return format.ToLower() switch
        {
            "pdf" => GeneratePdfReport(report.Name, selectedColumns),
            "excel" => GenerateExcelReport(report.Name, selectedColumns),
            "csv" => GenerateCsvReport(report.Name, selectedColumns),
            _ => BadRequest("Unsupported format")
        };
    }

    private async Task<IActionResult> DownloadReport(int reportId, string format)
    {
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null)
        {
            return NotFound();
        }

        var selectedColumns = report.SelectedColumns.Split(',');

        return format.ToLower() switch
        {
            "pdf" => GeneratePdfReport(report.Name, selectedColumns),
            "excel" => GenerateExcelReport(report.Name, selectedColumns),
            "csv" => GenerateCsvReport(report.Name, selectedColumns),
            _ => BadRequest("Unsupported format")
        };
    }

    private IActionResult GeneratePdfReport(string reportName, string[] columns)
    {
        var stream = new MemoryStream();

        // Create a new PDF document
        var document = new iTextSharp.text.Document();
        var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, stream);
        writer.CloseStream = false;

        document.Open();

        // Add title
        var titleFont = iTextSharp.text.FontFactory.GetFont("Arial", 18, iTextSharp.text.Font.BOLD);
        var titleParagraph = new iTextSharp.text.Paragraph(reportName, titleFont)
        {
            Alignment = iTextSharp.text.Element.ALIGN_CENTER,
            SpacingAfter = 20
        };
        document.Add(titleParagraph);

        // Add table
        var table = new iTextSharp.text.pdf.PdfPTable(columns.Length);

        // Add headers
        var headerFont = iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.BOLD);
        foreach (var column in columns)
        {
            var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(column, headerFont))
            {
                HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER,
                BackgroundColor = new iTextSharp.text.BaseColor(240, 240, 240)
            };
            table.AddCell(cell);
        }

        // Add sample data (replace with actual data from your context)
        var dataFont = iTextSharp.text.FontFactory.GetFont("Arial", 10);
        for (int row = 1; row <= 10; row++)
        {
            foreach (var column in columns)
            {
                var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase($"Data {row}", dataFont))
                {
                    HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER
                };
                table.AddCell(cell);
            }
        }

        document.Add(table);
        document.Close();
        writer.Flush();
        stream.Position = 0;

        return File(stream, "application/pdf", $"{reportName}.pdf");
    }

    private IActionResult GenerateExcelReport(string reportName, string[] columns)
    {
        var stream = new MemoryStream();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set the license context

        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add(reportName);

            // Add headers
            for (int i = 0; i < columns.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = columns[i];
            }

            // Add sample data (replace with actual data from your context)
            for (int row = 2; row <= 11; row++)
            {
                for (int col = 0; col < columns.Length; col++)
                {
                    worksheet.Cells[row, col + 1].Value = $"Data {row - 1}";
                }
            }

            package.SaveAs(stream);
        }

        stream.Position = 0;
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{reportName}.xlsx");
    }

    private IActionResult GenerateCsvReport(string reportName, string[] columns)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);

        // Add headers
        writer.WriteLine(string.Join(",", columns));

        // Add sample data (replace with actual data from your context)
        for (int row = 1; row <= 10; row++)
        {
            writer.WriteLine(string.Join(",", columns.Select(c => $"Data {row}")));
        }

        writer.Flush();
        stream.Position = 0;
        return File(stream, "text/csv", $"{reportName}.csv");
    }
}
