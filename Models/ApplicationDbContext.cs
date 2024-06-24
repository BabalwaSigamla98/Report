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
            modelBuilder.Entity<Report>()
                .HasOne(r => r.UserReport)
                .WithMany(ur => ur.Reports)
                .HasForeignKey(r => r.QueryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
