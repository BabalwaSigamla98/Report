using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using Lwesihlanu.Models;
using Microsoft.EntityFrameworkCore;

public class ReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<dynamic> GetDynamicReport(string[] selectedColumns)
    {
        if (selectedColumns == null || selectedColumns.Length == 0)
        {
            throw new ArgumentNullException(nameof(selectedColumns), "Selected columns cannot be null or empty.");
        }

        var query = _context.ReportColumns.AsQueryable();

        // Ensure client-side evaluation
        var queryAsEnumerable = query.AsEnumerable();

        var dynamicReports = queryAsEnumerable.Select(rc =>
        {
            var dynamicObject = new ExpandoObject() as IDictionary<string, object>;
            foreach (var columnName in selectedColumns)
            {
                var propertyInfo = typeof(ReportColumn).GetProperty(columnName);
                if (propertyInfo == null)
                {
                    throw new ArgumentException($"Column '{columnName}' does not exist in ReportColumn.");
                }
                dynamicObject[columnName] = propertyInfo.GetValue(rc);
            }
            return dynamicObject;
        });

        return dynamicReports;
    }

    public void SaveReport(string reportName, string description, string query, string[] selectedColumns, string filters, string format)
    {
        // Check if the query already exists in the UserReport table
        var existingUserReport = _context.UserReports
                                         .FirstOrDefault(ur => ur.QueryText == query);

        int queryId;
        if (existingUserReport == null)
        {
            // Save new UserReport
            var userReport = new UserReport
            {
                QueryText = query,
                SelectedColumns = string.Join(",", selectedColumns),
                Filters = filters,
                CreatedDate = DateTime.Now
            };

            _context.UserReports.Add(userReport);
            _context.SaveChanges();
            queryId = userReport.UserReportId;
        }
        else
        {
            // Use existing UserReportId
            queryId = existingUserReport.UserReportId;
        }

        // Save the new Report
        var report = new Report
        {
            Name = reportName,
            Description = description,
            SelectedColumns = string.Join(",", selectedColumns),
            Format = format,
            CreatedDate = DateTime.Now,
            QueryId = queryId
        };

        _context.Reports.Add(report);
        _context.SaveChanges();
    }
}
