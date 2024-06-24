using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lwesihlanu.Models
{
    public class ReportColumn
    {
        [Key]
        public int ColumnId { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }

        // Foreign key to Report
        public int ReportId { get; set; }
        [ForeignKey("ReportId")]
        public Report Report { get; set; }
    }
}
