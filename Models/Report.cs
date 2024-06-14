using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lwesihlanu.Models
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SelectedColumns { get; set; }
        public string Format { get; set; }
        public DateTime CreatedDate { get; set; }

        // Foreign key for UserReport
        public int QueryId { get; set; }

        // Navigation property
        public UserReport UserReport { get; set; }

        // Navigation property for related ReportColumns
        public ICollection<ReportColumn> ReportColumns { get; set; }
    }
}
