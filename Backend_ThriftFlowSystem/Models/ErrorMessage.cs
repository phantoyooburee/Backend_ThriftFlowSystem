using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ThriftFlowSystem.Models
{
    [Table("ErrorMessages")]
    public class ErrorMessage
    {
        [Key]
        public int ErrorCode { get; set; }

        [Required]
        [MaxLength(255)]
        public string ErrorDesc { get; set; } = string.Empty;
    }
}
