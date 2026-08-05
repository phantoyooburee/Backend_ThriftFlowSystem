using System.ComponentModel.DataAnnotations;

namespace Backend_ThriftFlowSystem.Models
{
    public class Branch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string BranchName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? LocationDetails { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
