using System.ComponentModel.DataAnnotations;

namespace Backend_ThriftFlowSystem.DTOs
{
    public class PromotionRequestDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string PromotionType { get; set; } = "PERCENT"; // PERCENT, FIXED, BUNDLE

        public decimal DiscountValue { get; set; } = 0;
        public int? ConditionQuantity { get; set; }
        public decimal? BundlePrice { get; set; }

        public int? ApplicableProductLotId { get; set; }
        public int? ApplicableCategoryId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}