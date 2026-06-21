using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend_ThriftFlowSystem.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Prefix { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        [JsonIgnore]
        public ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
