using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ThriftFlowSystem.Models
{
    [Table("SystemActionLogs")]
    public class SystemActionLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty; 

        [Required]
        [MaxLength(100)]
        public string TargetTable { get; set; } = string.Empty; 

        public int? TargetRecordId { get; set; } 

        [MaxLength(255)]
        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
    }
}
