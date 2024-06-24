using System.ComponentModel.DataAnnotations;

namespace Lwesihlanu.Models
{
    public class ReportColumn
    {
        [Key]
        public int ColumnId { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
}
