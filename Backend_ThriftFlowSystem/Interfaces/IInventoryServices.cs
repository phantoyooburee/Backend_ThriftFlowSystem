using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Models;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IInventoryServices
    {
        // Category
        Task<ResultListReply> GetCategoriesAsync(bool? isActive = null);
        Task<ResultListReply> CreateCategoryAsync(CategoryCreateRequest request, int employeeId);
        Task<ResultListReply> UpdateCategoryAsync(int id, CategoryUpdateRequest request, int employeeId, string pin);
        Task<ResultListReply> ToggleCategoryActiveAsync(int id, int employeeId, string pin);

        // Supplier
        Task<ResultListReply> GetSuppliersAsync();
        Task<ResultListReply> CreateSupplierAsync(SupplierCreateRequest request, int employeeId);
        Task<ResultListReply> UpdateSupplierAsync(int id, SupplierUpdateRequest request, int employeeId, string pin);
        Task<ResultListReply> ToggleSupplierActiveAsync(int id, int employeeId, string pin);

        // ProductLot
        Task<ResultListReply> GetProductLotsAsync(bool? isActive = null);
        Task<ResultListReply> CreateProductLotAsync(ProductLotCreateRequest request, int employeeId);
        Task<ResultListReply> UpdateProductLotAsync(int id, ProductLotUpdateRequest request, int employeeId, string pin);
        Task<ResultListReply> ToggleProductLotActiveAsync(int id, int employeeId, string pin);

        // Product
        Task<ResultListReply> GetProductByIdAsync(int id);
        Task<ResultListReply> GetProductsAsync(int page = 1, int pageSize = 20, string? search = null);
        Task<ResultListReply> CreateProductAsync(ProductCreateRequest request, int employeeId);
        Task<ResultListReply> UpdateProductAsync(int id, ProductUpdateRequest request, int employeeId, string pin);
        Task<ResultListReply> ToggleProductActiveAsync(int id, int employeeId, string pin);

        // Adjustment
        Task<ResultListReply> AdjustStockAsync(StockAdjustRequest request, int employeeId, string pin);
    }
}
