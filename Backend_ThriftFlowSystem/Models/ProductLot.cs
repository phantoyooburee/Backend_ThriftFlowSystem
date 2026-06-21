using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend_ThriftFlowSystem.Models
{
    [Table("ProductLots")]
    public class ProductLot
    {
        [Key]
        public int Id { get; set; }
        public int? SupplierId { get; set; }

        [Required]
        [MaxLength(150)]
        public string LotName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ColorTag { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalLotCost { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }

        [JsonIgnore]
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
