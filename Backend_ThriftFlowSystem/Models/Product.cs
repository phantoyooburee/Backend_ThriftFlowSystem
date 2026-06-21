using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend_ThriftFlowSystem.Models
{
    [Table("Products")]
    [Index(nameof(SKU), IsUnique = true)]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }


        [Required]
        public int ProductLotId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string SKU { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }
        public int QuantityInStock { get; set; } = 0;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        public bool IsGenericSKU { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
        
        [ForeignKey("ProductLotId")]
        public ProductLot? ProductLot { get; set; }

    }
}
