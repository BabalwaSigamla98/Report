using Microsoft.EntityFrameworkCore;
using Lwesihlanu.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var reports = _context.Reports.ToList();
        return View(reports);
    }

    public IActionResult Create()
    {
        var columns = _context.ReportColumns.ToList();
        return View(columns); // Ensure your view is strongly typed to IEnumerable<ReportColumn>
    }



    public async Task SaveReportConfiguration(string reportName, string description, string[] selectedColumnIds, string format)
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
        var reportColumns = selectedColumnIds.Select(id => new ReportColumn
        {
            ColumnId = int.Parse(id),
            ReportId = report.ReportId,
            IsSelected = true // Assuming selected columns are marked as true
        }).ToList();

        _context.ReportColumns.AddRange(reportColumns);
        await _context.SaveChangesAsync();
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
        var report = await _context.Reports.FindAsync(id);

        if (report == null)
        {
            return NotFound();
        }

        _context.Reports.Remove(report);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

}


//public IActionResult Clone(int id)
//{
//    var existingReport = _context.Reports.FirstOrDefault(r => r.ReportId == id);
//    if (existingReport == null)
//    {
//        return NotFound();
//    }

//    // Clone report
//    var clonedReport = new Report
//    {
//        Name = existingReport.Name + " (Clone)",
//        Description = existingReport.Description,
//        SelectedColumns = existingReport.SelectedColumns,
//        Format = existingReport.Format,
//        CreatedDate = DateTime.Now
//    };

//    _context.Reports.Add(clonedReport);
//    _context.SaveChanges();

//    // Clone report to UserReport table
//    var clonedUserReport = new UserReport
//    {
//        ReportId = clonedReport.ReportId,
//        SelectedColumns = clonedReport.SelectedColumns,
//        QueryText = JsonConvert.SerializeObject("Clone of " + existingReport.Name),
//        Filters = "tttttt", // Add filter logic if needed
//        CreatedDate = DateTime.Now
//    };

//    _context.UserReports.Add(clonedUserReport);
//    _context.SaveChanges();

//    return RedirectToAction(nameof(Index));
//}

//public async Task<IActionResult> Delete(int? id)
//{
//    if (id == null)
//    {
//        return NotFound();
//    }

//    var report = await _context.Reports
//        .FirstOrDefaultAsync(m => m.ReportId == id);
//    if (report == null)
//    {
//        return NotFound();
//    }

//    return View(report);
//}

//[HttpPost, ActionName("Delete")]
//[ValidateAntiForgeryToken]
//public async Task<IActionResult> DeleteConfirmed(int id)
//{
//    var report = await _context.Reports.FindAsync(id);
//    if (report == null)
//    {
//        return NotFound();
//    }

//    // Remove related UserReports
//    var userReports = _context.UserReports.Where(ur => ur.ReportId == id).ToList();
//    if (userReports.Any())
//    {
//        _context.UserReports.RemoveRange(userReports);
//    }

//    // Remove the report
//    _context.Reports.Remove(report);

//    await _context.SaveChangesAsync();
//    return RedirectToAction(nameof(Index));
// }


