using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Models;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IInventoryServices
    {
        // Category
        Task<ResultListReply> GetCategoriesAsync();
        Task<ResultListReply> CreateCategoryAsync(CategoryCreateRequest request, int employeeId);
        Task<ResultListReply> UpdateCategoryAsync(int id, CategoryUpdateRequest request, int employeeId);
        Task<ResultListReply> ToggleCategoryActiveAsync(int id, int employeeId);

        // Supplier
        Task<ResultListReply> GetSuppliersAsync();
        Task<ResultListReply> CreateSupplierAsync(SupplierCreateRequest request, int employeeId);
        Task<ResultListReply> UpdateSupplierAsync(int id, SupplierUpdateRequest request, int employeeId);
        Task<ResultListReply> ToggleSupplierActiveAsync(int id, int employeeId);

        // ProductLot
        Task<ResultListReply> GetProductLotsAsync();
        Task<ResultListReply> CreateProductLotAsync(ProductLotCreateRequest request, int employeeId);
        Task<ResultListReply> UpdateProductLotAsync(int id, ProductLotUpdateRequest request, int employeeId);
        Task<ResultListReply> ToggleProductLotActiveAsync(int id, int employeeId);

        // Product
        Task<ResultListReply> GetProductsAsync(int page = 1, int pageSize = 20, string? search = null);
        Task<ResultListReply> CreateProductAsync(ProductCreateRequest request, int employeeId);
        Task<ResultListReply> UpdateProductAsync(int id, ProductUpdateRequest request, int employeeId);
        Task<ResultListReply> ToggleProductActiveAsync(int id, int employeeId);

        // Adjustment
        Task<ResultListReply> AdjustStockAsync(StockAdjustRequest request, int employeeId);
    }
}
