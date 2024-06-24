using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lwesihlanu.Models
{
    public class UserReport
    {
        [Key]
        public int UserReportId { get; set; }
        public string SelectedColumns { get; set; }
        public string QueryText { get; set; }
        public string Filters { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation property for related reports
        public ICollection<Report> Reports { get; set; }
    }
}
