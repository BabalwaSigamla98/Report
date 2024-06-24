using Microsoft.EntityFrameworkCore;

namespace Lwesihlanu.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportColumn> ReportColumns { get; set; }
        public DbSet<UserReport> UserReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Report to UserReport relationship
            modelBuilder.Entity<Report>()
                .HasOne(r => r.UserReport)
                .WithMany(ur => ur.Reports)
                .HasForeignKey(r => r.QueryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ReportColumn to Report relationship
            modelBuilder.Entity<ReportColumn>()
                .HasOne(rc => rc.Report)
                .WithMany(r => r.ReportColumns)
                   .HasForeignKey(rc => rc.ReportId)
                .OnDelete(DeleteBehavior.Restrict);// Prevent cascade delete

        }
    }
}
