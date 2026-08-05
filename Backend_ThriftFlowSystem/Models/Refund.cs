using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ThriftFlowSystem.Models
{
    public class Refund
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        [MaxLength(255)]
        public string? Reason { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Order? Order { get; set; }
        public Product? Product { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
        public int? ApprovedById { get; set; }

        [ForeignKey("ApprovedById")]
        public Employee? ApprovedBy { get; set; }
    }
}