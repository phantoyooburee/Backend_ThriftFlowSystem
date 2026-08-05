using System.ComponentModel.DataAnnotations;

namespace Backend_ThriftFlowSystem.Models
{
    public class StoreProfile
    {
        [Key]
        public int Id { get; set; } = 1;

        [Required]
        [MaxLength(100)]
        public string StoreName { get; set; } = "THRIFT FLOW";

        public string Address { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(20)]
        public string TaxId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public string ReceiptFooter { get; set; } = "Thank you for shopping!";
    }
}
