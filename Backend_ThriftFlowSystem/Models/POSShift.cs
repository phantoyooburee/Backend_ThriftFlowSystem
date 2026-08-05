using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ThriftFlowSystem.Models
{
    public class POSShift
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; } 
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        public int BranchId { get; set; } 
        [ForeignKey("BranchId")]
        public Branch? Branch { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StartingCash { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashInAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashOutAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedCash { get; set; } = 0; 

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCash { get; set; } = 0; 

        [Column(TypeName = "decimal(18,2)")]
        public decimal Difference { get; set; } = 0; 

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "OPEN"; 

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }
}
