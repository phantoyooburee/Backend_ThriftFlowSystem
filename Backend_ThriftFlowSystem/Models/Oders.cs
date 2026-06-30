using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ThriftFlowSystem.Models

{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReceiptNumber { get; set; } = string.Empty;

        public int? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public Employee? ApprovedBy { get; set; }

        [Required]
        public int EmployeeId { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0; 

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; } 

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "CASH";

        [MaxLength(500)]
        public string? PaymentSlipUrl { get; set; } 

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "COMPLETED";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
        public int? PromotionId { get; set; }

        [ForeignKey("PromotionId")]
        public Promotion? Promotion { get; set; }

        [MaxLength(200)]
        public string? AppliedPromotionIds { get; set; }

        public bool IsSpecialPrice { get; set; } = false;

        public bool IsPromotionSkipped { get; set; } = false;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
