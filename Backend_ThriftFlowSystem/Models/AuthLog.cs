using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ThriftFlowSystem.Models
{
    public class AuthLog
    {
     [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        public int? EmployeeId { get; set; }

        public int? ActorId { get; set; }

        [MaxLength(100)]
        public string? TargetEmail { get; set; } 

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; } //such as browser info

        public string? Details { get; set; } 

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [ForeignKey("ActorId")]
        public Employee? ActorEmployee { get; set; }
    }
}
