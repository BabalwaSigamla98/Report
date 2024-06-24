using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lwesihlanu.Models;
using Newtonsoft.Json;

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
        return View(columns);
    }

    public async Task SaveReportConfiguration(string reportName, string description, string[] selectedColumnIds, string format)
    {
        var selectedColumns = _context.ReportColumns
                                      .Where(rc => selectedColumnIds.Contains(rc.ColumnId.ToString()))
                                      .Select(rc => rc.Name)
                                      .ToList();

        var selectedColumnsString = string.Join(",", selectedColumns);
        var queryJson = JsonConvert.SerializeObject(new { SelectedColumns = selectedColumns });

        var existingQuery = _context.UserReports
                                    .FirstOrDefault(ur => ur.QueryText == queryJson);

        int queryId;
        if (existingQuery == null)
        {
            var userReport = new UserReport
            {
                SelectedColumns = selectedColumnsString,
                QueryText = queryJson,
                Filters = "tttttt",
                CreatedDate = DateTime.Now
            };

            _context.UserReports.Add(userReport);
            await _context.SaveChangesAsync();
            queryId = userReport.UserReportId;
        }
        else
        {
            queryId = existingQuery.UserReportId;
        }

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
    }

 
    public async Task<IActionResult> Run(int id)
    {
        var report = await _context.Reports
            .Include(r => r.UserReport)
            .FirstOrDefaultAsync(r => r.ReportId == id);

        if (report == null)
        {
            return NotFound();
        }

        return View(report);
    }

    // GET: Reports/Clone/5
    public async Task<IActionResult> Clone(int id)
    {
        var existingReport = await _context.Reports
            .Include(r => r.UserReport)
            .FirstOrDefaultAsync(r => r.ReportId == id);

        if (existingReport == null)
        {
            return NotFound();
        }

        var clonedReport = new Report
        {
            Name = existingReport.Name + " (Clone)",
            Description = existingReport.Description,
            SelectedColumns = existingReport.SelectedColumns,
            Format = existingReport.Format,
            CreatedDate = DateTime.Now,
            QueryId = existingReport.QueryId
        };

        _context.Reports.Add(clonedReport);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Reports/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var report = await _context.Reports.FindAsync(id);
        if (report == null)
        {
            return NotFound();
        }

        return View(report);
    }


    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var report = await _context.Reports
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

    private bool ReportExists(int id)
    {
        return _context.Reports.Any(e => e.ReportId == id);
    }
}
