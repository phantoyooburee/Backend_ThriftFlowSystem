using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ThriftFlowSystem.Models
{
    [Table("Promotions")]
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Type: PERCENT, BUNDLE 
        [Required]
        [MaxLength(20)]
        public string PromotionType { get; set; } = "PERCENT";

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; } = 0;

        // For Pro BUNDLE such as 3 piece 100 Bath (ConditionQuantity = 3, BundlePrice = 100)
        public int? ConditionQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BundlePrice { get; set; }

        // filter: discount applicable to specific product lot or category or null for all products
        public int? ApplicableProductLotId { get; set; }
        public int? ApplicableCategoryId { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        public bool IsActive { get; set; } = true;

        [ForeignKey("ApplicableProductLotId")]
        public ProductLot? ApplicableProductLot { get; set; }

        [ForeignKey("ApplicableCategoryId")]
        public Category? ApplicableCategory { get; set; }
    }
}