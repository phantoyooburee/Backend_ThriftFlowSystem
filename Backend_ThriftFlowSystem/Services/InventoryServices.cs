using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace Backend_ThriftFlowSystem.Services
{
    public static class AppConstants
    {
        public const string StorageBucketProducts = "product-images";
        public const string DefaultCategoryPrefix = "TF";
    }

    public static class ActionTypes
    {
        public const string InRestock = "IN_RESTOCK";
        public const string OutDamage = "OUT_DAMAGE";
        public const string Adjust = "ADJUST"; 
        public const string Create = "CREATE";
        public const string CreateFail = "CREATE_FAIL";
        public const string Update = "UPDATE";
        public const string UpdateFail = "UPDATE_FAIL";
        public const string Restore = "RESTORE";
        public const string SoftDelete = "SOFT_DELETE";

        
        public static readonly string[] ValidAdjustActions = { InRestock, OutDamage, Adjust };
    }

    public class InventoryServices : IInventoryServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IResultReplyServices _reply;
        private readonly ILogger<InventoryServices> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly Supabase.Client _supabase;

        public InventoryServices(
            ApplicationDbContext context,
            IResultReplyServices reply,
            IWebHostEnvironment env,
            ILogger<InventoryServices> logger,
            Supabase.Client supabase)
        {
            _context = context;
            _reply = reply;
            _env = env;
            _logger = logger;
            _supabase = supabase;
        }

        // Category Services
        public async Task<ResultListReply> GetCategoriesAsync(bool? isActive = null)
        {
            var reply = new ResultListReply();
            try
            {
                var query = _context.Categories.AsQueryable();
                if (isActive.HasValue)
                    query = query.Where(c => c.IsActive == isActive.Value);

                var categories = await query
                    .Select(c => new CategoryResponse
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Prefix = c.Prefix,
                        IsActive = c.IsActive
                    }).ToListAsync();

                reply.Data = categories;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetCategoriesAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> CreateCategoryAsync(CategoryCreateRequest request, int employeeId)
        {
            var reply = new ResultListReply();
            try
            {
                bool isDuplicate = await _context.Categories.AnyAsync(c =>
                c.Name.ToLower() == request.Name.ToLower().Trim() ||
                c.Prefix.ToLower() == request.Prefix.ToLower().Trim());

                if (isDuplicate)
                {
                    var logFail = new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.CreateFail,
                        TargetTable = "Categories",
                        Details = $"Failed to create category: {request.Name} ({request.Prefix}) - Duplicate name or prefix"
                    };
                    _context.SystemActionLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Category name or prefix already exists.";
                    return reply;
                }

                var category = new Category
                {
                    Name = request.Name.Trim(),
                    Prefix = request.Prefix.ToUpper().Trim()
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                var log = new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Create, 
                    TargetTable = "Categories",
                    TargetRecordId = category.Id,
                    Details = $"Created category: {category.Name} ({category.Prefix})"
                };
                _context.SystemActionLogs.Add(log);
                await _context.SaveChangesAsync();

                reply.Data = new CategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name,
                    Prefix = category.Prefix,
                    IsActive = category.IsActive
                };
                reply.Result.ToSuccessStatus("201");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CreateCategoryAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> UpdateCategoryAsync(int id, CategoryUpdateRequest request, int employeeId, string pin)
        {
            var reply = new ResultListReply();
            try
            {
                if (!await IsPinValid(employeeId, pin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN. Permission denied.";
                    return reply;
                }
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Category Not found";
                    return reply;
                }
                bool isDuplicate = await _context.Categories.AnyAsync(c =>
                c.Id != id &&
                (c.Name.ToLower() == request.Name.ToLower().Trim() ||
                c.Prefix.ToLower() == request.Prefix.ToLower().Trim()));

                if (isDuplicate)
                {
                    var logFail = new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.UpdateFail, 
                        TargetTable = "Categories",
                        TargetRecordId = id,
                        Details = $"Failed to update category ID {id}: Name '{request.Name}' or Prefix '{request.Prefix}' already exists."
                    };
                    _context.SystemActionLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Category name or prefix already exists.";
                    return reply;
                }

                category.Name = request.Name.Trim();
                category.Prefix = request.Prefix.ToUpper().Trim();
                _context.Categories.Update(category);

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Update,
                    TargetTable = "Categories",
                    TargetRecordId = category.Id,
                    Details = $"Updated category ID {id} {category.Name} ({category.Prefix})"
                });

                await _context.SaveChangesAsync();

                reply.Data = new CategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name,
                    Prefix = category.Prefix,
                    IsActive = category.IsActive
                };

                //reply.Data = "Update successful";
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error UpdateCategory");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> ToggleCategoryActiveAsync(int id, int employeeId, string pin)
        {
            var reply = new ResultListReply();
            try
            {
                if (!await IsPinValid(employeeId, pin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN. Permission denied.";
                    return reply;
                }
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Category not found.";
                    return reply;
                }

                category.IsActive = !category.IsActive;
                _context.Categories.Update(category);

                string statusText = category.IsActive ? "Activated" : "Deactivated";

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = category.IsActive ? ActionTypes.Restore : ActionTypes.SoftDelete, // ✅ ใช้ Constant
                    TargetTable = "Categories",
                    TargetRecordId = id,
                    Details = $"Changed status of Category ID {id} {category.Name} ({category.Prefix}) to {statusText}"
                });
                await _context.SaveChangesAsync();

                reply.Data = statusText;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ToggleCategory");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        // Supplier Services
        public async Task<ResultListReply> GetSuppliersAsync()
        {
            var reply = new ResultListReply();
            try
            {
                var suppliers = await _context.Suppliers
                    .Select(s => new SupplierResponse
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ContactInfo = s.ContactInfo,
                        IsActive = s.IsActive
                    }).ToListAsync();

                reply.Data = suppliers;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetSuppliersAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> CreateSupplierAsync(SupplierCreateRequest request, int employeeId)
        {
            var reply = new ResultListReply();
            try
            {
                string nameToCheck = request.Name?.ToLower().Trim() ?? "";
                string? contactToCheck = request.ContactInfo?.ToLower().Trim();

                bool isDuplicate = await _context.Suppliers.AnyAsync(s =>
                s.Name.ToLower() == nameToCheck ||
                (contactToCheck != null && s.ContactInfo != null && s.ContactInfo.ToLower() == contactToCheck)
                );

                if (isDuplicate)
                {
                    var logFail = new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.CreateFail,
                        TargetTable = "Suppliers",
                        Details = $"Failed to create supplier: Name '{request.Name}' or Contact Info '{request.ContactInfo}' already exists."
                    };
                    _context.SystemActionLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Supplier name already exists.";
                    return reply;
                }

                var supplier = new Supplier
                {
                    Name = request.Name!.Trim(),
                    ContactInfo = request.ContactInfo?.Trim()
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                var log = new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Create,
                    TargetTable = "Suppliers",
                    TargetRecordId = supplier.Id,
                    Details = $"Created supplier: {supplier.Name}({supplier.ContactInfo})"
                };
                _context.SystemActionLogs.Add(log);
                await _context.SaveChangesAsync();

                reply.Data = new SupplierResponse
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactInfo = supplier.ContactInfo,
                    IsActive = supplier.IsActive
                };
                reply.Result.ToSuccessStatus("201");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CreateSupplierAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> UpdateSupplierAsync(int id, SupplierUpdateRequest request, int employeeId, string pin)
        {
            var reply = new ResultListReply();
            try
            {
                if (!await IsPinValid(employeeId, pin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN. Permission denied.";
                    return reply;
                }
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Supplier Not found";
                    return reply;
                }

                string nameToCheck = request.Name?.ToLower().Trim() ?? "";

                bool isDuplicate = await _context.Suppliers.AnyAsync(s =>
                s.Id != id && s.Name.ToLower() == nameToCheck);

                if (isDuplicate)
                {
                    var logFail = new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.UpdateFail,
                        TargetTable = "Suppliers",
                        TargetRecordId = id,
                        Details = $"Failed to update supplier ID {id}: Name '{request.Name}' already exists."
                    };
                    _context.SystemActionLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Supplier name already exists.";
                    return reply;
                }

                supplier.Name = request.Name!.Trim();
                supplier.ContactInfo = request.ContactInfo?.Trim();

                _context.Suppliers.Update(supplier);

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Update,
                    TargetTable = "Suppliers",
                    TargetRecordId = id,
                    Details = $"Updated Suppliers ID {id} {supplier.Name}({supplier.ContactInfo})"
                });
                await _context.SaveChangesAsync();

                //reply.Data = "Update successful";
                reply.Data = new SupplierResponse
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactInfo = supplier.ContactInfo,
                    IsActive = supplier.IsActive
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error UpdateSupplier");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> ToggleSupplierActiveAsync(int id, int employeeId, string pin)
        {
            var reply = new ResultListReply();
            try
            {
                if (!await IsPinValid(employeeId, pin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN. Permission denied.";
                    return reply;
                }
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                {
                    reply.Result.ToErrorStatus();
                    return reply;
                }

                supplier.IsActive = !supplier.IsActive;
                _context.Suppliers.Update(supplier);

                string statusText = supplier.IsActive ? "Activated" : "Deactivated";

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = supplier.IsActive ? ActionTypes.Restore : ActionTypes.SoftDelete, // ✅
                    TargetTable = "Suppliers",
                    TargetRecordId = id,
                    Details = $"Changed status Suppliers ID {id} {supplier.Name}({supplier.ContactInfo}) to {statusText}"
                });
                await _context.SaveChangesAsync();

                reply.Data = statusText;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ToggleSupplier");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        // ProductLot Services
        public async Task<ResultListReply> GetProductLotsAsync(bool? isActive = null)
        {
            var reply = new ResultListReply();
            try
            {
                var query = _context.ProductLots.AsQueryable();
                if (isActive.HasValue)
                    query = query.Where(l => l.IsActive == isActive.Value);

                var lots = await query
                    .Select(l => new ProductLotResponse
                    {
                        Id = l.Id,
                        SupplierId = l.SupplierId,
                        LotName = l.LotName,
                        ColorTag = l.ColorTag,
                        TotalLotCost = l.TotalLotCost,
                        ReceivedQuantity = l.ReceivedQuantity,
                        CostPerUnit = l.CostPerUnit,
                        ReceivedDate = l.ReceivedDate,
                        AllocatedQuantity = l.AllocatedQuantity,
                        IsActive = l.IsActive
                    }).ToListAsync();

                reply.Data = lots;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetProductLotsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> CreateProductLotAsync(ProductLotCreateRequest request, int employeeId)
        {
            var reply = new ResultListReply();
            try
            {
                bool supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == request.SupplierId);
                if (!supplierExists)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Supplier not found.";
                    return reply;
                }

                bool isDuplicate = await _context.ProductLots.AnyAsync(l =>
                l.LotName.ToLower() == request.LotName.ToLower().Trim());

                if (isDuplicate)
                {
                    var logFail = new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.CreateFail,
                        TargetTable = "ProductLots",
                        Details = $"Failed to create product lot: Name '{request.LotName}' already exists."
                    };
                    _context.SystemActionLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Product lot name already exists.";
                    return reply;
                }

                var lot = new ProductLot
                {
                    SupplierId = request.SupplierId,
                    LotName = request.LotName.Trim(),
                    ColorTag = request.ColorTag?.Trim(),
                    TotalLotCost = request.TotalLotCost,
                    ReceivedQuantity = request.ReceivedQuantity,
                    CostPerUnit = request.ReceivedQuantity > 0 ? request.TotalLotCost / request.ReceivedQuantity : 0
                };

                _context.ProductLots.Add(lot);
                await _context.SaveChangesAsync();

                var log = new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Create,
                    TargetTable = "ProductLots",
                    TargetRecordId = lot.Id,
                    Details = $"Created product lot: {lot.LotName} with cost {lot.TotalLotCost}"
                };
                _context.SystemActionLogs.Add(log);
                await _context.SaveChangesAsync();

                reply.Data = new ProductLotResponse
                {
                    Id = lot.Id,
                    SupplierId = lot.SupplierId,
                    LotName = lot.LotName,
                    ColorTag = lot.ColorTag,
                    TotalLotCost = lot.TotalLotCost,
                    ReceivedQuantity = lot.ReceivedQuantity,
                    CostPerUnit = lot.CostPerUnit,
                    ReceivedDate = lot.ReceivedDate,
                    IsActive = lot.IsActive
                };
                reply.Result.ToSuccessStatus("201");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CreateProductLotAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> UpdateProductLotAsync(int id, ProductLotUpdateRequest request, int employeeId, string pin)
        {
            var reply = new ResultListReply();
            try
            {
                if (!await IsPinValid(employeeId, pin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN. Permission denied.";
                    return reply;
                }
                var lot = await _context.ProductLots.FindAsync(id);
                if (lot == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Product lot not found.";
                    return reply;
                }

                bool supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == request.SupplierId);
                if (!supplierExists)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Supplier not found.";
                    return reply;
                }

                bool isDuplicate = await _context.ProductLots.AnyAsync(l =>
                l.Id != id && l.LotName.ToLower() == request.LotName.ToLower().Trim());

                if (isDuplicate)
                {
                    var logFail = new SystemActionLog
                    {
                        EmployeeId = employeeId,
                        ActionType = ActionTypes.UpdateFail,
                        TargetTable = "ProductLots",
                        TargetRecordId = id,
                        Details = $"Failed to update lot ID {id}: Name '{request.LotName}' already exists."
                    };
                    _context.SystemActionLogs.Add(logFail);
                    await _context.SaveChangesAsync();

                    reply.Result.ToErrorStatus();
                    reply.Data = "Product lot name already exists.";
                    return reply;
                }

                lot.SupplierId = request.SupplierId;
                lot.LotName = request.LotName.Trim();
                lot.ColorTag = request.ColorTag?.Trim();
                lot.TotalLotCost = request.TotalLotCost;
                lot.ReceivedQuantity = request.ReceivedQuantity;
                lot.CostPerUnit = request.ReceivedQuantity > 0 ? request.TotalLotCost / request.ReceivedQuantity : 0;
                _context.ProductLots.Update(lot);

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Update,
                    TargetTable = "ProductLots",
                    TargetRecordId = id,
                    Details = $"Updated Product Lot ID {id} to {lot.LotName} ({lot.ColorTag}) cost {lot.TotalLotCost}"
                });

                await _context.SaveChangesAsync();

                //reply.Data = "Update successful";
                reply.Data = new ProductLotResponse
                {
                    Id = lot.Id,
                    SupplierId = lot.SupplierId,
                    LotName = lot.LotName,
                    ColorTag = lot.ColorTag,
                    TotalLotCost = lot.TotalLotCost,
                    ReceivedQuantity = lot.ReceivedQuantity,
                    CostPerUnit = lot.CostPerUnit,
                    ReceivedDate = lot.ReceivedDate,
                    IsActive = lot.IsActive
                };

                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error UpdateLot");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> ToggleProductLotActiveAsync(int id, int employeeId, string pin)
        {
            var reply = new ResultListReply();
            try
            {
                if (!await IsPinValid(employeeId, pin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN. Permission denied.";
                    return reply;
                }
                var lot = await _context.ProductLots.FindAsync(id);
                if (lot == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Product lot not found.";
                    return reply;
                }

                lot.IsActive = !lot.IsActive;
                _context.ProductLots.Update(lot);

                string statusText = lot.IsActive ? "Activated" : "Deactivated";

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = lot.IsActive ? ActionTypes.Restore : ActionTypes.SoftDelete, 
                    TargetTable = "ProductLots",
                    TargetRecordId = id,
                    Details = $"Changed status of Product Lot ID {id} {lot.LotName} to {statusText}"
                });
                await _context.SaveChangesAsync();

                reply.Data = statusText;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ToggleLot");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }


        // Product Services
        public async Task<ResultListReply> GetProductsAsync(int page = 1, int pageSize = 20, string? search = null)
        {
            var reply = new ResultListReply();
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductLot)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string searchTerm = search.ToLower().Trim();
                    query = query.Where(p => p.Name.ToLower().Contains(searchTerm) || p.SKU.ToLower().Contains(searchTerm));
                }

                int totalItems = await query.CountAsync();

                var products = await query
                    .OrderByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Width = p.Width,
                        Length = p.Length,
                        NeckTag = p.NeckTag,
                        SKU = p.SKU,
                        SellingPrice = p.SellingPrice,
                        QuantityInStock = p.QuantityInStock,
                        ImageUrl = p.ImageUrl,
                        ProductLotId = p.ProductLotId,
                        ProductLotName = p.ProductLot != null ? p.ProductLot.LotName : "Unknown",
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category != null ? p.Category.Name : "Unknown",
                        IsGenericSKU = p.IsGenericSKU,
                        IsActive = p.IsActive
                    }).ToListAsync();

                reply.Data = new PagedProductResponse
                {
                    Items = products,
                    TotalItems = totalItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetProductsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> GetProductByIdAsync(int id)
        {
            var reply = new ResultListReply();
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductLot)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = $"ProductId : {id} not found.";
                    return reply;
                }

                reply.Data = new ProductResponse
                {
                    Id = product.Id,
                    CategoryId = product.CategoryId,
                    ProductLotId = product.ProductLotId,
                    Name = product.Name,
                    Width = product.Width,
                    Length = product.Length,
                    NeckTag = product.NeckTag,
                    Detail = product.Detail,
                    SKU = product.SKU,
                    SellingPrice = product.SellingPrice,
                    QuantityInStock = product.QuantityInStock,
                    ImageUrl = product.ImageUrl,
                    ProductLotName = product.ProductLot?.LotName ?? "Unknown",
                    CategoryName = product.Category?.Name ?? "Unknown",
                    IsGenericSKU = product.IsGenericSKU,
                    IsActive = product.IsActive
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetProductById");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> CreateProductAsync(ProductCreateRequest request, int employeeId)
        {
            var reply = new ResultListReply();
            string? uploadedFileName = null;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                if (request.SellingPrice <= 0)
                {
                    await transaction.RollbackAsync();
                    reply.Result.ToErrorStatus();
                    reply.Data = "SellingPrice can not value = 0.";
                    return reply;
                }

                var category = await _context.Categories.FindAsync(request.CategoryId);
                if (category == null || !category.IsActive)
                {
                    await transaction.RollbackAsync();
                    reply.Result.ToErrorStatus();
                    reply.Data = "Category not found.";
                    return reply;
                }

                var productLot = await _context.ProductLots.FindAsync(request.ProductLotId);
                if (productLot == null || !productLot.IsActive)
                {
                    await transaction.RollbackAsync();
                    reply.Result.ToErrorStatus();
                    reply.Data = "Product lot not found.";
                    return reply;
                }

                if ((productLot.AllocatedQuantity + request.InitialQuantity) > productLot.ReceivedQuantity)
                {
                    await transaction.RollbackAsync();
                    reply.Result.ToErrorStatus();
                    reply.Data = $"Invalid Quantity:ProductLot '{productLot.LotName}' Not enouge (Used {productLot.AllocatedQuantity}, Input {productLot.ReceivedQuantity})";
                    return reply;
                }
                string? uploadedImageUrl = null;

                if (request.ImageFile != null && request.ImageFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await request.ImageFile.CopyToAsync(memoryStream);
                    var fileExtension = Path.GetExtension(request.ImageFile.FileName);

                    uploadedFileName = $"products/{Guid.NewGuid()}{fileExtension}";

                    await _supabase.Storage
                        .From(AppConstants.StorageBucketProducts)
                        .Upload(memoryStream.ToArray(), uploadedFileName, new Supabase.Storage.FileOptions { Upsert = false });

                    uploadedImageUrl = _supabase.Storage.From(AppConstants.StorageBucketProducts).GetPublicUrl(uploadedFileName);
                }

                var categoryPrefix = !string.IsNullOrWhiteSpace(category.Prefix) ? category.Prefix : AppConstants.DefaultCategoryPrefix;

                string finalSKU = string.IsNullOrWhiteSpace(request.SKU)
                    ? $"{categoryPrefix.ToUpper()}-{GenerateShortRandomCode(4)}"
                    : request.SKU.ToUpper().Trim();

                var product = new Product
                {
                    CategoryId = request.CategoryId,
                    ProductLotId = request.ProductLotId,
                    Name = request.Name!.Trim(),
                    Width = request.Width,
                    Length = request.Length,
                    NeckTag = request.NeckTag?.Trim(),
                    Detail = request.Detail?.Trim(),
                    SKU = finalSKU,
                    SellingPrice = request.SellingPrice,
                    QuantityInStock = request.InitialQuantity,
                    IsGenericSKU = request.IsGenericSKU,
                    ImageUrl = uploadedImageUrl,
                    IsActive = request.InitialQuantity != 0
                };

                var log = new InventoryLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.InRestock,
                    QuantityChanged = request.InitialQuantity,
                    Note = "Add Product to System",
                    Product = product
                };

                productLot.AllocatedQuantity += request.InitialQuantity;
                if (productLot.AllocatedQuantity >= productLot.ReceivedQuantity)
                {
                    productLot.IsActive = false;
                }
                _context.ProductLots.Update(productLot);
                _context.Products.Add(product);
                _context.InventoryLogs.Add(log);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                reply.Data = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Width = product.Width,
                    Length = product.Length,
                    NeckTag = product.NeckTag,
                    SKU = product.SKU,
                    Detail = product.Detail,
                    SellingPrice = product.SellingPrice,
                    QuantityInStock = product.QuantityInStock,
                    ImageUrl = product.ImageUrl,
                    ProductLotId = product.ProductLotId,
                    ProductLotName = productLot.LotName,
                    CategoryId = product.CategoryId,
                    CategoryName = category.Name,
                    IsGenericSKU = product.IsGenericSKU,
                    IsActive = product.IsActive

                };

                reply.Result.ToSuccessStatus("201");
                reply.ToSuccessStatus();
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database Error - Possible Duplicate SKU");

                if (!string.IsNullOrEmpty(uploadedFileName))
                    await _supabase.Storage.From(AppConstants.StorageBucketProducts).Remove(new List<string> { uploadedFileName });

                reply.Result.ToErrorStatus();
                reply.Data = "Failed to save product. SKU might be duplicated. Please try again.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error CreateProductAsync");

                if (!string.IsNullOrEmpty(uploadedFileName))
                    await _supabase.Storage.From(AppConstants.StorageBucketProducts).Remove(new List<string> { uploadedFileName });

                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> UpdateProductAsync(int id, ProductUpdateRequest request, int employeeId, string pin)
        {
            var reply = new ResultListReply();
            try
            {
                if (request.SellingPrice <= 0)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "SellingPrice can not value = 0.";
                    return reply;
                }

                if (!await IsPinValid(employeeId, pin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN. Permission denied.";
                    return reply;
                }
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Product not found.";
                    return reply;
                }
                var category = await _context.Categories.FindAsync(request.CategoryId);
                if (category == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Category not found.";
                    return reply;
                }
                if (request.CategoryId != product.CategoryId && !category.IsActive)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Can not change to this Category Disabled  ";
                    return reply;
                }
                var productLot = await _context.ProductLots.FindAsync(request.ProductLotId);
                if (productLot == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Product lot not found.";
                    return reply;
                }
                //Image upload handling
                if (request.ImageFile != null && request.ImageFile.Length > 0)
                {
                    string? oldImageUrl = product.ImageUrl;

                    using var ms = new MemoryStream();
                    await request.ImageFile.CopyToAsync(ms);
                    var fileName = $"products/{Guid.NewGuid()}{Path.GetExtension(request.ImageFile.FileName)}";
                    await _supabase.Storage.From(AppConstants.StorageBucketProducts).Upload(ms.ToArray(), fileName, new Supabase.Storage.FileOptions { Upsert = false });

                    product.ImageUrl = _supabase.Storage.From(AppConstants.StorageBucketProducts).GetPublicUrl(fileName);
                    if (!string.IsNullOrEmpty(oldImageUrl))
                    {
                        try
                        {
                            
                            var oldFileName = oldImageUrl.Split("/storage/v1/object/public/product-images/").LastOrDefault();
                            if (!string.IsNullOrEmpty(oldFileName))
                            {
                                await _supabase.Storage.From(AppConstants.StorageBucketProducts).Remove(new List<string> { oldFileName });
                            }
                        }
                        catch (Exception ex)
                        {
                            
                            _logger.LogWarning(ex, $"Failed to delete old image from Supabase: {oldImageUrl}");
                        }
                    }
                }


                product.CategoryId = request.CategoryId;
                //product.ProductLotId = request.ProductLotId;
                product.Name = request.Name!.Trim();
                product.Width = request.Width;
                product.Length = request.Length;
                product.NeckTag = request.NeckTag?.Trim();
                product.Detail = request.Detail?.Trim();
                product.SellingPrice = request.SellingPrice;
                product.IsGenericSKU = request.IsGenericSKU;

                _context.Products.Update(product);

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = ActionTypes.Update,
                    TargetTable = "Products",
                    TargetRecordId = id,
                    Details = $"Updated Product ID {id}: {product.Name} (SKU: {product.SKU})"
                });
                await _context.SaveChangesAsync();

                //reply.Data = "Update successful";
                reply.Data = new ProductResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Width = product.Width,
                    Length = product.Length,
                    NeckTag = product.NeckTag,
                    Detail = product.Detail,
                    SKU = product.SKU,
                    SellingPrice = product.SellingPrice,
                    QuantityInStock = product.QuantityInStock,
                    ImageUrl = product.ImageUrl,
                    ProductLotId = product.ProductLotId,
                    ProductLotName = productLot.LotName,
                    CategoryId = product.CategoryId,
                    CategoryName = category.Name,
                    IsGenericSKU = product.IsGenericSKU,
                    IsActive = product.IsActive
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error UpdateProduct");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> ToggleProductActiveAsync(int id, int employeeId, string pin)
        {
            var reply = new ResultListReply();
            try
            {
                if (!await IsPinValid(employeeId, pin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN. Permission denied.";
                    return reply;
                }
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Product not found.";
                    return reply;
                }

                product.IsActive = !product.IsActive;
                _context.Products.Update(product);

                string statusText = product.IsActive ? "Activated" : "Deactivated";
                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = product.IsActive ? ActionTypes.Restore : ActionTypes.SoftDelete,
                    TargetTable = "Products",
                    TargetRecordId = id,
                    Details = $"Changed status of Product ID {id} ({product.SKU}) to {statusText}"
                });
                await _context.SaveChangesAsync();

                reply.Data = statusText;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ToggleProduct");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        private string GenerateShortRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Adjust Stock Service
        public async Task<ResultListReply> AdjustStockAsync(StockAdjustRequest request, int employeeId, string pin)
        {
            var reply = new ResultListReply();
            try
            {
                if (!await IsPinValid(employeeId, pin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN. Permission denied.";
                    return reply;
                }
                string incomingAction = request.ActionType.ToUpper().Trim();
                if (!ActionTypes.ValidAdjustActions.Contains(incomingAction))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = $"Invalid ActionType. Allowed values are: {string.Join(", ", ActionTypes.ValidAdjustActions)}";
                    return reply;
                }

                //var product = await _context.Products.FindAsync(request.ProductId);
                var product = await _context.Products
                    .Include(p => p.ProductLot)
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId);
                if (product == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Product not found.";
                    return reply;
                }

                if (request.Quantity > 0 && product.ProductLot != null)
                {
                    if ((product.ProductLot.AllocatedQuantity + request.Quantity) > product.ProductLot.ReceivedQuantity)
                    {
                        reply.Result.ToErrorStatus();
                        reply.Data = $"Invalid Quantity: ProductLot '{product.ProductLot.LotName}' not enouge (The product has already been cut off {product.ProductLot.AllocatedQuantity}, Input {product.ProductLot.ReceivedQuantity})";
                        return reply;
                    }
                }
                int oldQuantity = product.QuantityInStock;

                //int rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                //UPDATE ""Products""
                //SET ""QuantityInStock"" = ""QuantityInStock"" + {request.Quantity}
                //WHERE ""Id"" = {request.ProductId}
                //AND ""QuantityInStock"" + {request.Quantity} >= 0");

                //if (rowsAffected == 0)
                //{
                //    reply.Result.ToErrorStatus();
                //    reply.Data = "Not enough stock, or stock changed by another transaction. Please retry.";
                //    return reply;
                //}

                await _context.Entry(product).ReloadAsync();
                int newQuantity = product.QuantityInStock;

                bool wasActive = product.IsActive;
                if (newQuantity <= 0)
                {
                    product.IsActive = false;
                }
                else if (incomingAction == ActionTypes.InRestock)
                {
                    product.IsActive = true;
                }

                if (wasActive != product.IsActive)
                {
                    _context.Products.Update(product);
                }
                if (product.ProductLot != null && request.Quantity > 0)
                {
                    product.ProductLot.AllocatedQuantity += request.Quantity;

                    if (product.ProductLot.AllocatedQuantity >= product.ProductLot.ReceivedQuantity)
                    {
                        product.ProductLot.IsActive = false; 
                    }
                    _context.ProductLots.Update(product.ProductLot);
                }

                var log = new InventoryLog
                {
                    EmployeeId = employeeId,
                    ActionType = incomingAction,
                    QuantityChanged = request.Quantity,
                    Note = !string.IsNullOrWhiteSpace(request.Note) ? request.Note.Trim() : "Manual Stock Adjustment",
                    ProductId = product.Id
                };
                _context.InventoryLogs.Add(log);
                await _context.SaveChangesAsync();

                reply.Data = new
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    OldQuantity = oldQuantity,
                    NewQuantity = newQuantity,
                    AdjustedBy = request.Quantity,
                    Action = log.ActionType
                };

                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error AdjustStockAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        private async Task<bool> IsPinValid(int employeeId, string? inputPin)
        {
            if (string.IsNullOrEmpty(inputPin)) return false;

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null || string.IsNullOrEmpty(employee.PinHash))
                return false;

            return BCrypt.Net.BCrypt.Verify(inputPin, employee.PinHash);
        }
    }
}