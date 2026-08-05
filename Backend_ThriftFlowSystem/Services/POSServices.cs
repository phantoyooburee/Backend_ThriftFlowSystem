
using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend_ThriftFlowSystem.Services
{
    public class POSServices : IPOSServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IResultReplyServices _reply;
        private readonly ILogger<POSServices> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly Supabase.Client _supabase;
        private readonly IInventoryServices _inventoryServices;

        public POSServices(
            ApplicationDbContext context,
            IResultReplyServices reply,
            ILogger<POSServices> logger,
            IWebHostEnvironment env,
            Supabase.Client supabase,
            IInventoryServices inventoryServices)
        {
            _context = context;
            _reply = reply;
            _logger = logger;
            _env = env;
            _supabase = supabase;
            _inventoryServices = inventoryServices;
        }

        public async Task<ResultListReply> CheckoutAsync(CheckoutRequest request, int employeeId)
        {
            var reply = new ResultListReply();
            string? uploadedFileName = null;
            List<OrderItemRequestDto> requestItems;

            // แปลงข้อมูล JSON จากตะกร้าสินค้าหน้าบ้าน
            try
            {
                string cleanJson = request.OrderItemsJson?.Trim() ?? "[]";

                if (cleanJson.StartsWith("\"") && cleanJson.EndsWith("\""))
                {
                    cleanJson = cleanJson.Trim('"');
                    cleanJson = cleanJson.Replace("\\\"", "\"");
                }

                if (cleanJson.StartsWith("{") && cleanJson.EndsWith("}"))
                {
                    cleanJson = $"[{cleanJson}]";
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                requestItems = JsonSerializer.Deserialize<List<OrderItemRequestDto>>(cleanJson, options) ?? new List<OrderItemRequestDto>();

                if (!requestItems.Any())
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Cart is empty. Please add items to checkout.";
                    return reply;
                }

                if (requestItems.Any(item => item.Quantity <= 0))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid item quantity. All quantities must be greater than zero.";
                    return reply;
                }

                // AUTO-MERGE: ถ้ายิงบาร์โค้ดหางผ้าซ้ำกันมา 
                requestItems = requestItems
                    .GroupBy(x => x.ProductId)
                    .Select(g => new OrderItemRequestDto
                    {
                        ProductId = g.Key,
                        Quantity = g.Sum(x => x.Quantity)
                    })
                    .ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Failed to parse OrderItemsJson. Raw Data: {request.OrderItemsJson}");
                reply.Result.ToErrorStatus();
                reply.Data = "Invalid order items data format.";
                return reply;
            }

            //CheckSlip (option only TRANSFER)
            //if (request.PaymentMethod.ToUpper() == "TRANSFER" && (request.SlipImage == null || request.SlipImage.Length == 0))
            //{
            //    reply.Result.ToErrorStatus();
            //    reply.Data = "Payment slip image is required for TRANSFER method.";
            //    return reply;
            //}

            //Check Permissions — Dont use SpecialPrice and SkipPromotion both
            if (request.SkipPromotion && request.SpecialPrice.HasValue)
            {
                reply.Result.ToErrorStatus();
                reply.Data = "Cannot combine SkipPromotion and Special Price in the same order.";
                return reply;
            }

            if (request.SpecialPrice.HasValue && request.SpecialPrice.Value < 0)
            {
                reply.Result.ToErrorStatus();
                reply.Data = "Special price cannot be negative.";
                return reply;
            }

            // Save ID (Manager/Owner)
            int? approverId = null;

            bool requiresManagerCheck = request.SpecialPrice.HasValue || request.SkipPromotion;

            if (requiresManagerCheck)
            {
                if (string.IsNullOrWhiteSpace(request.ManagerPin))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = request.SkipPromotion
                        ? "Manager PIN is required to skip promotions."
                        : "Manager PIN is required for special price.";
                    return reply;
                }


                var eligibleManagers = await _context.Employees
                    .Include(e => e.Role)
                    .Where(e => e.Role != null && (e.Role.Level == 1 || e.Role.Level == 2))
                    .ToListAsync();

                var manager = eligibleManagers.FirstOrDefault(m =>
                    BCrypt.Net.BCrypt.Verify(request.ManagerPin, m.PinHash)
                );

                if (manager == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid PIN or insufficient permissions.";
                    return reply;
                }


                approverId = manager.Id;
            }

            var activeShift = await _context.POSShifts
                .FirstOrDefaultAsync(s => s.BranchId == request.BranchId && s.Status == "OPEN");

            if (activeShift == null)
            {
                reply.Result.ToErrorStatus();
                reply.Data = "No open shift for this branch. Please have a manager open a shift first.";
                return reply;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //var (totalAmount, finalDiscountAmount, netAmount, appliedPromotionIds, pricedItems) =
                //await CalculatePricingAsync(requestItems, request.SkipPromotion, request.SpecialPrice);
                var (totalAmount, finalDiscountAmount, netAmount, appliedPromotionIds, pricedItems) =
                await CalculatePricingAsync(requestItems, request.SkipPromotion, request.SpecialPrice);

                decimal? changeDue = null;
                if (request.PaymentMethod.ToUpper() == "CASH")
                {
                    if (!request.CashReceived.HasValue)
                        throw new Exception("Cash received amount is required for CASH payment.");

                    if (request.CashReceived.Value < netAmount)
                        throw new Exception($"Cash received is not enough. Required: {netAmount}, Received: {request.CashReceived.Value}");

                    changeDue = request.CashReceived.Value - netAmount;
                }

                bool isSpecialPriceApplied = request.SpecialPrice.HasValue;
                //decimal totalAmount = 0;
                var orderItemsToSave = new List<OrderItem>();
                var inventoryLogsToSave = new List<InventoryLog>();

                foreach (var pi in pricedItems)
                {
                    var product = pi.Product;

                    int rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"
                    UPDATE ""Products""
                    SET ""QuantityInStock"" = ""QuantityInStock"" - {pi.Request.Quantity}
                    WHERE ""Id"" = {product.Id} 
                    AND ""QuantityInStock"" - {pi.Request.Quantity} >= 0");

                    if (rowsAffected == 0)
                    {
                        throw new Exception($"Insufficient stock for productId: {product.Id} '{product.Name}' (SKU: {product.SKU}). Stock changed by another transaction.");
                    }

                    await _context.Entry(product).ReloadAsync();
                    if (product.QuantityInStock <= 0 && product.IsActive)
                    {
                        product.IsActive = false;
                        _context.Products.Update(product);
                    }

                    inventoryLogsToSave.Add(new InventoryLog
                    {
                        EmployeeId = employeeId,
                        ActionType = "OUT_SALE",
                        QuantityChanged = -pi.Request.Quantity,
                        Note = "Sold via POS Checkout",
                        ProductId = product.Id
                    });

                    orderItemsToSave.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = pi.Request.Quantity,
                        UnitPrice = product.SellingPrice,
                        SubTotal = pi.FullLineTotal,
                        CostPrice = product.ProductLot != null ? product.ProductLot.CostPerUnit : 0
                    });
                }

                var order = new Order
                {
                    EmployeeId = employeeId,
                    ApprovedById = approverId,
                    POSShiftId = activeShift.Id,
                    BranchId = activeShift.BranchId,
                    TotalAmount = totalAmount,
                    DiscountAmount = finalDiscountAmount,
                    NetAmount = netAmount,
                    PaymentMethod = request.PaymentMethod.ToUpper(),
                    CashReceived = request.PaymentMethod.ToUpper() == "CASH" ? request.CashReceived : null,
                    ChangeDue = changeDue,
                    PaymentSlipUrl = null,
                    Status = "COMPLETED",
                    PromotionId = appliedPromotionIds.FirstOrDefault() == 0 ? null : appliedPromotionIds.FirstOrDefault(),
                    AppliedPromotionIds = appliedPromotionIds.Any() ? string.Join(",", appliedPromotionIds) : null,
                    IsSpecialPrice = isSpecialPriceApplied,
                    IsPromotionSkipped = request.SkipPromotion,
                    OrderItems = orderItemsToSave
                };

                _context.Orders.Add(order);
                _context.InventoryLogs.AddRange(inventoryLogsToSave);

                await _context.SaveChangesAsync();

                order.ReceiptNumber = $"REC-{DateTime.UtcNow:yyyyMMdd}-{order.Id:D4}";

                foreach (var log in inventoryLogsToSave)
                {
                    log.Note = $"Receipt: {order.ReceiptNumber}";
                }

                // upload Slip Supabase

                if (request.SlipImage != null && request.SlipImage.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await request.SlipImage.CopyToAsync(memoryStream);
                    var fileExtension = Path.GetExtension(request.SlipImage.FileName);

                    uploadedFileName = $"slips/{order.ReceiptNumber}/{Guid.NewGuid()}{fileExtension}";

                    await _supabase.Storage
                        .From("payment-slips")
                        .Upload(memoryStream.ToArray(), uploadedFileName, new Supabase.Storage.FileOptions { Upsert = false });

                    order.PaymentSlipUrl = _supabase.Storage.From("payment-slips").GetPublicUrl(uploadedFileName);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                reply.Data = new
                {
                    OrderId = order.Id,
                    ReceiptNumber = order.ReceiptNumber,
                    TotalAmount = order.TotalAmount,
                    DiscountAmount = order.DiscountAmount,
                    NetAmount = order.NetAmount,
                    PaymentMethod = order.PaymentMethod,
                    CashReceived = order.CashReceived,
                    ChangeDue = changeDue,
                    PaymentSlipUrl = order.PaymentSlipUrl,
                    AppliedPromotionIds = appliedPromotionIds,
                    IsSpecialPrice = order.IsSpecialPrice,
                    IsPromotionSkipped = order.IsPromotionSkipped,
                    ApprovedById = approverId,
                    CreatedAt = order.CreatedAt,
                    ItemCount = orderItemsToSave.Sum(i => i.Quantity)
                };

                reply.Result.ToSuccessStatus("201");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during POS Checkout");

                if (!string.IsNullOrEmpty(uploadedFileName))
                {
                    try
                    {
                        await _supabase.Storage.From("payment-slips").Remove(new List<string> { uploadedFileName });
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, $"Failed to delete slip from Supabase: {uploadedFileName}");
                    }
                }

                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }

            return reply;
        }

        public async Task<ResultListReply> UploadSlipLaterAsync(int orderId, IFormFile slipImage, int employeeId)
        {
            var reply = new ResultListReply();
            string? uploadedFileName = null;
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Order not found.";
                    return reply;
                }

                if (slipImage == null || slipImage.Length == 0)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "No slip image provided.";
                    return reply;
                }

  
                using var memoryStream = new MemoryStream();
                await slipImage.CopyToAsync(memoryStream);
                var fileExtension = Path.GetExtension(slipImage.FileName);

  
                uploadedFileName = $"slips/{order.ReceiptNumber}/{Guid.NewGuid()}{fileExtension}";

                await _supabase.Storage
                    .From("payment-slips")
                    .Upload(memoryStream.ToArray(), uploadedFileName, new Supabase.Storage.FileOptions { Upsert = false });

                string slipUrl = _supabase.Storage.From("payment-slips").GetPublicUrl(uploadedFileName);

                order.PaymentSlipUrl = slipUrl;
                _context.Orders.Update(order);


                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId,
                    ActionType = "UPDATE",
                    TargetTable = "Orders",
                    TargetRecordId = order.Id,
                    Details = $"Uploaded payment slip for Order {order.ReceiptNumber} later."
                });

                await _context.SaveChangesAsync();

                reply.Data = new { OrderId = order.Id, SlipUrl = slipUrl, Message = "Slip uploaded successfully." };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error UploadSlipLaterAsync");


                if (!string.IsNullOrEmpty(uploadedFileName))
                {
                    try { await _supabase.Storage.From("payment-slips").Remove(new List<string> { uploadedFileName }); }
                    catch { /* Ignore */ }
                }

                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred while uploading the slip.";
            }
            return reply;
        }
        //Preview Cart Before Checkout
        public async Task<ResultListReply> CalculateCartAsync(CalculateCartRequest request)
        {
            var reply = new ResultListReply();
            try
            {
                if (request.Items == null || !request.Items.Any())
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Cart is empty.";
                    return reply;
                }

                if (request.Items.Any(i => i.Quantity <= 0))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid item quantity. All quantities must be greater than zero.";
                    return reply;
                }

                var mergedItems = request.Items
                    .GroupBy(x => x.ProductId)
                    .Select(g => new OrderItemRequestDto { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                    .ToList();

                var (totalAmount, discountAmount, netAmount, appliedPromotionIds, pricedItems) =
                    await CalculatePricingAsync(mergedItems, skipPromotion: false, specialPrice: null);

                reply.Data = new CartPreviewResponse
                {
                    TotalAmount = totalAmount,
                    DiscountAmount = discountAmount,
                    NetAmount = netAmount,
                    AppliedPromotionIds = appliedPromotionIds,
                    Items = pricedItems.Select(pi => new CartItemPreview
                    {
                        ProductId = pi.Product.Id,
                        Name = pi.Product.Name,
                        SKU = pi.Product.SKU,
                        Quantity = pi.Request.Quantity,
                        UnitPrice = pi.Product.SellingPrice,
                        FullLineTotal = pi.FullLineTotal,
                        DiscountedLineTotal = pi.DiscountedLineTotal,
                        AppliedPromotionId = pi.AssignedPromotionId
                    }).ToList()
                };

                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CalculateCartAsync");
                reply.Result.ToErrorStatus();
                reply.Data = ex.Message;
            }
            return reply;
        }

        private async Task<(decimal totalAmount, decimal discountAmount, decimal netAmount, List<int> appliedPromotionIds, List<PricedItem> pricedItems)>
            CalculatePricingAsync(List<OrderItemRequestDto> requestItems, bool skipPromotion, decimal? specialPrice)
        {
            decimal totalAmount = 0;
            var pricedItems = new List<PricedItem>();

            foreach (var item in requestItems)
            {
                var product = await _context.Products
                    .Include(p => p.ProductLot)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null || !product.IsActive)
                {
                    throw new Exception($"Product ID {item.ProductId} not found or inactive.");
                }

                if (product.QuantityInStock < item.Quantity)
                {
                    throw new Exception($"Insufficient stock for productId: {item.ProductId} '{product.Name}' (SKU: {product.SKU}). Remaining: {product.QuantityInStock}");
                }

                totalAmount += product.SellingPrice * item.Quantity;

                pricedItems.Add(new PricedItem
                {
                    Product = product,
                    Request = item,
                    DiscountedLineTotal = product.SellingPrice * item.Quantity
                });
            }

            decimal finalDiscountAmount = 0;
            var appliedPromotionIds = new List<int>();
            bool shouldApplyPromotions = !skipPromotion && !specialPrice.HasValue;

            if (shouldApplyPromotions)
            {

                (finalDiscountAmount, pricedItems) = await ApplyPromotionsAsync(pricedItems, appliedPromotionIds);

            }

            decimal netAmount;
            if (specialPrice.HasValue)
            {
                if (specialPrice.Value > totalAmount)
                    throw new Exception("Special price cannot be greater than the total amount.");

                netAmount = specialPrice.Value;
                finalDiscountAmount = totalAmount - netAmount;
            }
            else
            {
                netAmount = totalAmount - finalDiscountAmount;
            }

            if (netAmount < 0) netAmount = 0;

            return (totalAmount, finalDiscountAmount, netAmount, appliedPromotionIds, pricedItems);
        }

        private async Task<(decimal, List<PricedItem>)> ApplyPromotionsAsync(List<PricedItem> originalItems, List<int> appliedPromotionIds)
        {
            var now = DateTime.UtcNow;
            var activePromotions = await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync();

            if (!activePromotions.Any()) return (0, originalItems);

            // แตกสินค้าทุกชิ้นเป็นชิ้นเดี่ยวใน Memory (Flattening)
            var units = new List<UnitItem>();
            foreach (var pi in originalItems)
            {
                for (int i = 0; i < pi.Request.Quantity; i++)
                {
                    units.Add(new UnitItem
                    {
                        Product = pi.Product,
                        OriginalPrice = pi.Product.SellingPrice,
                        DiscountedPrice = pi.Product.SellingPrice,
                        AssignedPromotionId = null
                    });
                }
            }

            decimal totalDiscount = 0;
            var appliedIds = new HashSet<int>();

            // เรียงลำดับโปรโมชั่น เอา BUNDLE ขึ้นก่อนPERCENT เสมอและเรียงความเจาะจง
            var sortedPromos = activePromotions
                .OrderByDescending(p => p.PromotionType == "BUNDLE")
                .ThenBy(p => GetSpecificityRank(p))
                .ThenByDescending(p => p.Id)
                .ToList();

            foreach (var promo in sortedPromos)
            {
                // หาสินค้า ที่ยังไม่ได้โปร และเข้าเงื่อนไข
                var eligibleUnits = units.Where(u => u.AssignedPromotionId == null && IsProductEligible(u.Product, promo)).ToList();

                if (!eligibleUnits.Any()) continue;

                if (promo.PromotionType == "BUNDLE" && promo.ConditionQuantity.HasValue && promo.BundlePrice.HasValue)
                {
                    int condQty = promo.ConditionQuantity.Value;
                    if (eligibleUnits.Count >= condQty)
                    {
                        // นำของราคาถูกที่สุดมาเข้าเซ็ตก่อนเพื่อรักษากำไรของร้าน Margin
                        var sortedEligible = eligibleUnits.OrderBy(u => u.OriginalPrice).ToList();
                        int sets = sortedEligible.Count / condQty;
                        int unitsToApply = sets * condQty;

                        decimal bundlePrice = promo.BundlePrice.Value;
                        decimal normalPriceOfBundledUnits = sortedEligible.Take(unitsToApply).Sum(u => u.OriginalPrice);
                        decimal discountForThisPromo = normalPriceOfBundledUnits - (sets * bundlePrice);
                        if (discountForThisPromo < 0) discountForThisPromo = 0;

                        // อัปเดตราคาสินค้าที่ถูกดึงเข้าเซ็ต
                        for (int i = 0; i < unitsToApply; i++)
                        {
                            var u = sortedEligible[i];
                            u.AssignedPromotionId = promo.Id;
                            decimal shareRatio = normalPriceOfBundledUnits == 0 ? 0 : u.OriginalPrice / normalPriceOfBundledUnits;
                            u.DiscountedPrice = u.OriginalPrice - Math.Round(discountForThisPromo * shareRatio, 2);
                        }
                        totalDiscount += discountForThisPromo;
                        appliedIds.Add(promo.Id);
                    }
                }
                else if (promo.PromotionType == "PERCENT")
                {
                    // เศษที่รอดจาก Bundle จะไหลมาโดนตรงนี้แทน
                    foreach (var u in eligibleUnits)
                    {
                        u.AssignedPromotionId = promo.Id;
                        decimal discount = Math.Round(u.OriginalPrice * (promo.DiscountValue / 100m), 2);
                        u.DiscountedPrice = u.OriginalPrice - discount;
                        totalDiscount += discount;
                        appliedIds.Add(promo.Id);
                    }
                }
            }

            // รวบรวมชิ้นเดี่ยว กลับเป็นบรรทัดใบเสร็จ จัดกลุ่มตาม ID สินค้า และ โปรโมชั่นที่ได้รับ
            var newPricedItems = new List<PricedItem>();
            var groupedUnits = units.GroupBy(u => new { u.Product.Id, u.AssignedPromotionId });

            foreach (var g in groupedUnits)
            {
                var firstUnit = g.First();
                int qty = g.Count();
                decimal discountedTotal = g.Sum(u => u.DiscountedPrice);

                newPricedItems.Add(new PricedItem
                {
                    Product = firstUnit.Product,
                    Request = new OrderItemRequestDto { ProductId = firstUnit.Product.Id, Quantity = qty },
                    DiscountedLineTotal = discountedTotal,
                    AssignedPromotionId = firstUnit.AssignedPromotionId
                });
            }

            appliedPromotionIds.AddRange(appliedIds);
            return (totalDiscount, newPricedItems);
        }

        private bool IsProductEligible(Product product, Promotion promo)
        {
            if (!product.IsGenericSKU && promo.PromotionType == "BUNDLE") return false;
            if (promo.ApplicableProductLotId.HasValue && product.ProductLotId != promo.ApplicableProductLotId.Value) return false;
            if (promo.ApplicableCategoryId.HasValue && product.CategoryId != promo.ApplicableCategoryId.Value) return false;
            return true;
        }

        private int GetSpecificityRank(Promotion promo)
        {
            if (promo.ApplicableProductLotId.HasValue) return 1; // เจาะจงสุด
            if (promo.ApplicableCategoryId.HasValue) return 2;
            return 3; // ทั้งร้าน กว้างที่สุด
        }

        //เก็บผลลัพธ์การคำนวณส่วนลดของสินค้า 1 ชิ้น ใช้ภายใน CheckoutAsync เท่านั้น
        private class PricedItem
        {
            public required Product Product { get; set; }
            public required OrderItemRequestDto Request { get; set; }
            public decimal FullLineTotal => Product.SellingPrice * Request.Quantity;
            public decimal DiscountedLineTotal { get; set; } // จะถูกตั้งค่าตอนคำนวณ BUNDLE/PERCENT
            public int? AssignedPromotionId { get; set; }    // null = ไม่เข้าโปรไหนเลย คิดราคาเต็ม
        }

        private class UnitItem
        {
            public required Product Product { get; set; }
            public decimal OriginalPrice { get; set; }
            public decimal DiscountedPrice { get; set; }
            public int? AssignedPromotionId { get; set; }
        }

        public async Task<ResultListReply> SearchOrderByReceiptAsync(string receiptNumber)
        {
            var reply = new ResultListReply();
            try
            {
                if (string.IsNullOrWhiteSpace(receiptNumber))
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Receipt number is required.";
                    return reply;
                }

                // ดึงข้อมูลบิล, รายการสินค้า และประวัติการคืนเงิน
                var order = await _context.Orders
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Include(o => o.Refunds)
                    .FirstOrDefaultAsync(o => o.ReceiptNumber == receiptNumber.Trim());

                if (order == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Order not found.";
                    return reply;
                }

                
                var response = new
                {
                    OrderId = order.Id,
                    ReceiptNumber = order.ReceiptNumber,
                    TotalAmount = order.TotalAmount,
                    NetAmount = order.NetAmount,
                    PaymentMethod = order.PaymentMethod,      
                    CashReceived = order.CashReceived,
                    ChangeDue = order.ChangeDue,
                    CreatedAt = order.CreatedAt,
                    Items = order.OrderItems.Select(oi => {

                        // คำนวณยอดที่คืนได้สูงสุด
                        int refundedQty = order.Refunds?
                       .Where(r => r.ProductId == oi.ProductId)
                       .Sum(r => r.Quantity) ?? 0;
                        // สัดส่วนราคาที่จ่ายจริง
                        decimal paidRatio = order.TotalAmount > 0 ? (order.NetAmount / order.TotalAmount) : 0;
                        // ราคาต่อหน่วยที่ลูกค้าจ่ายจริง  หลังหักส่วนลดแล้ว  
                        decimal unitPriceAfterDiscount = Math.Round(oi.UnitPrice * paidRatio, 2);
                        return new
                        {
                            ProductId = oi.ProductId,
                            Name = oi.Product?.Name ?? "Unknown",
                            SKU = oi.Product?.SKU ?? "N/A",
                            PurchasedQuantity = oi.Quantity,
                            RefundedQuantity = refundedQty,
                            RemainingRefundable = oi.Quantity - refundedQty,
                            UnitPrice = oi.UnitPrice,
                            EffectiveUnitPrice = unitPriceAfterDiscount,
                            SubTotal = oi.SubTotal,
                            EffectiveSubTotal = Math.Round(unitPriceAfterDiscount * oi.Quantity, 2)
                        };
                    }).ToList()
                };

                reply.Data = response;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error SearchOrderByReceiptAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }

            return reply;
        }

        public async Task<ResultListReply> ProcessRefundAsync(RefundRequestDto request, int employeeId)
        {
            var reply = new ResultListReply();
            int? approverId = null;

            if (string.IsNullOrWhiteSpace(request.ManagerPin))
            {
                reply.Result.ToErrorStatus();
                reply.Data = "Manager PIN is required to process a refund.";
                return reply;
            }
            var eligibleManagers = await _context.Employees
            .Include(e => e.Role)
            .Where(e => e.Role != null && (e.Role.Level == 1 || e.Role.Level == 2))
            .ToListAsync();

            var manager = eligibleManagers.FirstOrDefault(m =>
            BCrypt.Net.BCrypt.Verify(request.ManagerPin, m.PinHash));

            if (manager == null)
            {
                reply.Result.ToErrorStatus();
                reply.Data = "Invalid PIN or insufficient permissions. Manager or Owner approval is required.";
                return reply;
            }
            approverId = manager.Id;

            var order = await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Refunds)
            .FirstOrDefaultAsync(o => o.Id == request.OriginalOrderId);

            if (order == null)
            {
                reply.Result.ToErrorStatus();
                reply.Data = "Original order not found.";
                return reply;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var refundsToSave = new List<Refund>();

                // วนลูปจัดการสินค้าที่ขอคืนทีละตัว
                foreach (var item in request.Items)
                {
                    var orderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == item.ProductId);
                    if (orderItem == null)
                    {
                        throw new Exception($"Product ID {item.ProductId} was not found in this order.");
                    }

                    // คำนวณยอดที่เคยคืนไปแล้ว เพื่อบล็อกการคืนเกินจำนวน
                    int alreadyRefundedQty = order.Refunds
                        .Where(r => r.ProductId == item.ProductId)
                        .Sum(r => r.Quantity);

                    if (alreadyRefundedQty + item.Quantity > orderItem.Quantity)
                    {
                        throw new Exception($"Cannot refund {item.Quantity} units of Product ID {item.ProductId}. Only {orderItem.Quantity - alreadyRefundedQty} units left eligible.");
                    }

                    var stockRequest = new StockAdjustRequest
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        ActionType = ActionTypes.InReturn,
                        Note = $"Refund from Order #{request.OriginalOrderId}: {request.Reason}"
                    };

                    var stockResult = await _inventoryServices.AdjustStockAsync(stockRequest, approverId.Value, request.ManagerPin);
                    if (stockResult.Result.Value == "F")
                    {
                        throw new Exception($"Stock Adjustment Failed for Product {item.ProductId}: {stockResult.Data}");
                    }

                    decimal itemFullPrice = orderItem.UnitPrice * item.Quantity; 

                    // ป้องกันหารด้วย 0 และคำนวณสัดส่วน 
                    decimal paidRatio = order.TotalAmount > 0 ? (order.NetAmount / order.TotalAmount) : 0;

                    // ยอดที่ต้องควักเงินคืนลูกค้าจริง
                    decimal actualRefundAmount = Math.Round(itemFullPrice * paidRatio, 2);

                    refundsToSave.Add(new Refund
                    {
                        OrderId = request.OriginalOrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        RefundAmount = actualRefundAmount, 
                        Reason = request.Reason,
                        EmployeeId = employeeId,
                        ApprovedById = approverId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _context.Refunds.AddRange(refundsToSave);
                await _context.SaveChangesAsync();

                var allRefunds = await _context.Refunds
                .Where(r => r.OrderId == request.OriginalOrderId)
                .ToListAsync();

                bool isFullyRefunded = order.OrderItems.All(oi =>
                allRefunds.Where(r => r.ProductId == oi.ProductId)
                .Sum(r => r.Quantity) >= oi.Quantity);

                // Refunded_คืนั้งหมด, PartialRefund_คืนบางส่วน
                order.Status = isFullyRefunded ? "REFUNDED" : "PARTIAL_REFUNDED";

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                reply.Data = new
                {
                    Refunds = refundsToSave.Select(r => new
                    {
                        Id = r.Id,
                        OrderId = r.OrderId,
                        ProductId = r.ProductId,
                        Quantity = r.Quantity,
                        RefundAmount = r.RefundAmount,
                        Reason = r.Reason,
                        CreatedAt = r.CreatedAt
                        
                    }),

                    
                    ApprovedBy = new
                    {
                        Id = manager.Id,
                        Name = manager.FirstName,
                        RoleName = manager.Role?.RoleName ?? "Unknown"
                    }
                };
                reply.Result.ToSuccessStatus("201");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error ProcessRefundAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred while processing the refund.";
            }

            return reply;
        }

        public async Task<ResultListReply> GetActiveShiftAsync(int branchId)
        {
            var reply = new ResultListReply();
            try
            {
                var activeShift = await _context.POSShifts
                    .Include(s => s.Branch)
                    .FirstOrDefaultAsync(s => s.BranchId == branchId && s.Status == "OPEN");

                if (activeShift == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "No active shift found for this branch.";
                    return reply;
                }

                reply.Data = activeShift;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
                return reply;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetActiveShiftAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred while processing the GetActiveShift.";
            }
            return reply;
        }

        public async Task<ResultListReply> OpenShiftAsync(int employeeId, OpenShiftRequest request)
        {
            var reply = new ResultListReply();

            try
            {
                var existingShift = await _context.POSShifts
                    .FirstOrDefaultAsync(s => s.BranchId == request.BranchId && s.Status == "OPEN");

                if (existingShift != null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "This branch already has an open shift.";
                    return reply;
                }

                var newShift = new POSShift
                {
                    EmployeeId = employeeId,
                    BranchId = request.BranchId,
                    StartingCash = request.StartingCash,
                    ExpectedCash = request.StartingCash, // Expected = Starting
                    ActualCash = 0,
                    Difference = 0,
                    Status = "OPEN",
                    StartTime = DateTime.UtcNow
                };

                _context.POSShifts.Add(newShift);
                await _context.SaveChangesAsync();

                reply.Data = new { ShiftId = newShift.Id, Message = "Shift opened successfully." };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error OpenShiftAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred while OpenShiftAsync.";
            }
            return reply;
        }

        public async Task<ResultListReply> CloseShiftAsync(int shiftId, int employeeId, CloseShiftRequest request)
        {
            var reply = new ResultListReply();
            try
            {
                var shift = await _context.POSShifts.FirstOrDefaultAsync(s => s.Id == shiftId);

                if (shift == null || shift.Status == "CLOSED")
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Shift not found or already closed.";
                    return reply;
                }

                // คำนวณยอดขาย "เงินสด" ทั้งหมดที่เกิดขึ้นในกะนี้
                var totalCashSales = await _context.Orders
                    .Where(o => o.POSShiftId == shiftId && o.Status == "COMPLETED" && o.PaymentMethod == "CASH")
                    .SumAsync(o => o.NetAmount);

                // ยอดเงินที่ควรมี = ทอนตั้งต้น + ยอดขายเงินสด + เงินที่เติมเข้า - เงินที่ดึงออก
                shift.ExpectedCash = shift.StartingCash + totalCashSales + shift.CashInAmount - shift.CashOutAmount;

                shift.ActualCash = request.ActualCash;

                // ส่วนต่าง = นับได้จริง - ที่ควรมี (ถ้าติดลบแปลว่าเงินหาย)
                shift.Difference = shift.ActualCash - shift.ExpectedCash;

                shift.EndTime = DateTime.UtcNow;
                shift.Status = "CLOSED";
                shift.Remarks = $"[Closed by EmpID: {employeeId}] " + request.Remarks;

                _context.POSShifts.Update(shift);
                await _context.SaveChangesAsync();

                reply.Data = new
                {
                    Message = "Shift closed successfully.",
                    ExpectedCash = shift.ExpectedCash,
                    ActualCash = shift.ActualCash,
                    Difference = shift.Difference
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CloseShiftAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred while CloseShiftAsync.";
            }
            return reply;
        }

        public async Task<ResultListReply> AddCashTransactionAsync(int branchId, int employeeId, CashTransactionRequest request)
        {
            var reply = new ResultListReply();
            try
            {
                int? approverId = null;
                string? approverName = null;
                if (request.TransactionType.ToUpper() == "CASH_OUT")
                {
                    if (string.IsNullOrWhiteSpace(request.ManagerPin))
                    {
                        reply.Result.ToErrorStatus();
                        reply.Data = "Manager PIN is required for Cash Out.";
                        return reply;
                    }

                    var eligibleManagers = await _context.Employees
                        .Include(e => e.Role)
                        .Where(e => e.Role != null && (e.Role.Level == 1 || e.Role.Level == 2))
                        .ToListAsync();

                    var manager = eligibleManagers.FirstOrDefault(m =>
                        BCrypt.Net.BCrypt.Verify(request.ManagerPin, m.PinHash)
                    );

                    if (manager == null)
                    {
                        reply.Result.ToErrorStatus();
                        reply.Data = "Invalid PIN or insufficient permissions. Manager or Owner approval is required for Cash Out.";
                        return reply;
                    }

                    approverId = manager.Id;
                    approverName = $"{manager.FirstName} {manager.LastName}".Trim(); ;
                }

                var activeShift = await _context.POSShifts
                    .FirstOrDefaultAsync(s => s.BranchId == branchId && s.Status == "OPEN");

                if (activeShift == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "No active shift found. Please open a shift first.";
                    return reply;
                }

                if (request.TransactionType.ToUpper() == "CASH_IN")
                {
                    activeShift.CashInAmount += request.Amount;
                }
                else if (request.TransactionType.ToUpper() == "CASH_OUT")
                {
                    activeShift.CashOutAmount += request.Amount;
                }
                else
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Invalid TransactionType. Use 'CASH_IN' or 'CASH_OUT'.";
                    return reply;
                }

                string approverLog = approverId.HasValue ? $" | ApprovedBy: {approverName}" : "";

                _context.SystemActionLogs.Add(new SystemActionLog
                {
                    EmployeeId = employeeId, 
                    ActionType = request.TransactionType.ToUpper(),
                    TargetTable = "POSShifts",
                    TargetRecordId = activeShift.Id,
                    Details = $"Amount: {request.Amount} | Remark: {request.Remarks}{approverLog}"
                });

                _context.POSShifts.Update(activeShift);
                await _context.SaveChangesAsync();

                reply.Data = new
                {
                    Message = "Cash transaction recorded successfully.",
                    CurrentCashIn = activeShift.CashInAmount,
                    CurrentCashOut = activeShift.CashOutAmount
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddCashTransactionAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred while processing cash transaction.";
            }

            return reply;
        }
    }
}