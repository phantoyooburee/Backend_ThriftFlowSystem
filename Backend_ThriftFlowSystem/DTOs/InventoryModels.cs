
using System.ComponentModel.DataAnnotations;

namespace Backend_ThriftFlowSystem.DTOs
{
    public class InventoryModels
    {
    }
    // DTOs for Category
    public class CategoryCreateRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Prefix { get; set; } = string.Empty;
    }
    public class CategoryUpdateRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Prefix { get; set; } = string.Empty;
    }
    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    // DTOs for Supplier
    public class SupplierCreateRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
    }
    public class SupplierUpdateRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
    }
    public class SupplierResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
        public bool IsActive { get; set; }
    }

    // DTOs for ProductLot
    public class ProductLotCreateRequest
    {
        public int? SupplierId { get; set; }
        [Required]
        public string LotName { get; set; } = string.Empty;
        public string? ColorTag { get; set; }
        [Required]
        public decimal TotalLotCost { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int ReceivedQuantity { get; set; }
    }
    public class ProductLotUpdateRequest
    {
        public int? SupplierId { get; set; }
        [Required] 
        public string LotName { get; set; } = string.Empty;
        public string? ColorTag { get; set; }
        [Required] 
        public decimal TotalLotCost { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int ReceivedQuantity { get; set; }

    }
    public class ProductLotResponse : ProductLotCreateRequest
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public int AllocatedQuantity { get; set; }
        public decimal CostPerUnit { get; set; }
    }

    // DTOs for Product
    public class ProductCreateRequest
    {
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public int ProductLotId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public double? Width { get; set; }
        public double? Length { get; set; }
        public string? NeckTag { get; set; }
        public string? Detail { get; set; }
        public string? SKU { get; set; } = string.Empty;
        [Required]
        public decimal SellingPrice { get; set; }
        public int InitialQuantity { get; set; }
        public bool IsGenericSKU { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
    public class ProductUpdateRequest 
    {
        [Required] 
        public int CategoryId { get; set; }

        [Required] 
        public int ProductLotId { get; set; }

        [Required] 
        public string Name { get; set; } = string.Empty;
        public double? Width { get; set; }
        public double? Length { get; set; }
        public string? NeckTag { get; set; }
        public string? Detail { get; set; }

        [Required] 
        public decimal SellingPrice { get; set; }
        public bool IsGenericSKU { get; set; }
        public IFormFile? ImageFile { get; set; } 
    }
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double? Width { get; set; }
        public double? Length { get; set; }
        public string? NeckTag { get; set; }
        public string? Detail { get; set; }
        public string SKU { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public int QuantityInStock { get; set; }
        public string? ImageUrl { get; set; }
        public int ProductLotId { get; set; }
        public string ProductLotName { get; set; } = string.Empty;
        public string? ColorTag { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsGenericSKU { get; set; }
        public bool IsActive { get; set; }
    }

    public class PagedProductResponse
    {
        public List<ProductResponse> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // DTOs for Adjustment
    public class StockAdjustRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; } // Fill in the quantity to adjust (positive for increase, negative for decrease)

        [Required]
        public string ActionType { get; set; } = null!; 

        public string? Note { get; set; } // Mention the reason for the adjustment (optional)
    }
}
