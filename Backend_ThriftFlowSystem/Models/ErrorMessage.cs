using System.ComponentModel.DataAnnotations;

namespace Backend_ThriftFlowSystem.Models
{
    public class ErrorMessage
    {
        [Key]
        public int ErrorCode { get; set; }

        [Required]
        [MaxLength(255)]
        public string ErrorDesc { get; set; } = string.Empty;
    }
}
